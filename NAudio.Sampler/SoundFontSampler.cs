using System;
using System.Collections.Generic;
using NAudio.SoundFont;
using NAudio.Wave;

namespace NAudio.Sampler
{
    /// <summary>
    /// A polyphonic software sampler that plays a <see cref="SoundFont"/> in
    /// response to MIDI note and controller events, rendering 32-bit float stereo
    /// through the standard NAudio <see cref="ISampleProvider"/> pull model. Drive
    /// it live (feed live MIDI-in events) or offline (feed a <see cref="Midi.MidiFile"/>'s
    /// events on a schedule and render to a WAV).
    ///
    /// Built on the shared <see cref="SamplerEngine"/>: pitch, looping, DAHDSR
    /// amplitude and modulation envelopes, two per-voice LFOs, a modulated
    /// resonant low-pass filter, the SoundFont modulator engine (default + file
    /// modulators), reverb/chorus sends, voice stealing, exclusive-class choke,
    /// and channel state (program/bank, pitch-bend, sustain, controllers).
    /// </summary>
    public sealed class SoundFontSampler : SamplerEngine
    {
        private readonly SoundFont.SoundFont soundFont;
        private readonly float[] samplePool;

        // projected, playable regions per preset key (bank<<16 | program)
        private readonly Dictionary<int, IReadOnlyList<SamplerRegion>> regionCache = new();

        /// <summary>
        /// Creates a sampler for the given SoundFont.
        /// </summary>
        /// <param name="soundFont">The loaded SoundFont to play.</param>
        /// <param name="sampleRate">Output sample rate in Hz (default 44100).</param>
        /// <param name="maxVoices">Maximum simultaneous voices (default 64).</param>
        public SoundFontSampler(SoundFont.SoundFont soundFont, int sampleRate = 44100, int maxVoices = 64)
            : base(sampleRate, maxVoices)
        {
            this.soundFont = soundFont ?? throw new ArgumentNullException(nameof(soundFont));
            samplePool = ConvertSampleData(soundFont);
        }

        /// <summary>The SoundFont bank holding GM percussion kits.</summary>
        public const int PercussionBank = 128;

        /// <summary>
        /// The MIDI channel (0-based) treated as the GM percussion channel: its
        /// notes always resolve against the percussion bank (<see cref="PercussionBank"/> = 128),
        /// regardless of any bank-select messages — note numbers pick the drum, not
        /// a pitch. Defaults to 9 (MIDI channel 10). Set to -1 to disable, e.g. for
        /// a non-GM SoundFont with no percussion bank.
        /// </summary>
        public int PercussionChannel { get; set; } = 9;

        /// <inheritdoc />
        private protected override IReadOnlyList<SamplerRegion> GetRegionsForNoteOn(int channel, MidiChannelState state)
        {
            // GM channel 10 is always percussion: force bank 128 so a stray
            // bank-select on the drum track can't drop it onto a melodic preset
            bool percussion = channel == PercussionChannel;
            int bank = percussion ? PercussionBank : state.Bank;

            int key = (bank << 16) | state.Program;
            if (regionCache.TryGetValue(key, out var cached)) return cached;

            var preset = percussion ? FindPercussionPreset(state.Program) : FindPreset(bank, state.Program);
            var resolved = preset?.ResolveRegions();
            IReadOnlyList<SamplerRegion> regions = resolved == null ? null : Project(resolved);
            regionCache[key] = regions;
            return regions;
        }

        // a percussion kit must come from the percussion bank — never fall back to
        // a melodic preset. Prefer the requested kit, else the Standard Kit (program 0).
        private Preset FindPercussionPreset(int program)
        {
            Preset standardKit = null;
            foreach (var p in soundFont.Presets)
            {
                if (p.Bank != PercussionBank) continue;
                if (p.PatchNumber == program) return p;
                if (p.PatchNumber == 0) standardKit ??= p;
            }
            return standardKit;
        }

        // projects each resolved SoundFont region onto the format-neutral region the
        // voice plays: the shared sample pool sliced by the sample header, the
        // generators as-is, and the combined (default + file) modulator list
        private List<SamplerRegion> Project(IReadOnlyList<SoundFontRegion> regions)
        {
            var result = new List<SamplerRegion>(regions.Count);
            foreach (var region in regions)
            {
                var sh = region.Sample;

                // Skip regions whose sample data isn't present/valid rather than
                // letting the voice's reader throw on an out-of-range slice. This is
                // what makes ROM-based SoundFonts (samples in hardware ROM, so smpl
                // is empty) load and play as silence instead of failing.
                if (sh.Start >= sh.End || sh.End > samplePool.Length) continue;

                result.Add(new SamplerRegion
                {
                    Sample = new SampleData
                    {
                        Data = samplePool,
                        Start = (int)sh.Start,
                        End = (int)sh.End,
                        LoopStart = (int)sh.StartLoop,
                        LoopEnd = (int)sh.EndLoop,
                        SampleRate = (int)sh.SampleRate,
                        RootKey = sh.OriginalPitch,
                        PitchCorrectionCents = sh.PitchCorrection
                    },
                    Generators = region.Generators,
                    Modulators = ModulatorSet.Build(region),
                    VelocityTrackingPercent = 0f, // SF2 velocity is driven by the modulator list
                    // exclusiveClass is a self-choking group (a note silences others in its class)
                    Group = region.Generators.ExclusiveClass,
                    OffByGroup = region.Generators.ExclusiveClass,
                    LoKey = region.LowKey,
                    HiKey = region.HighKey,
                    LoVelocity = region.LowVelocity,
                    HiVelocity = region.HighVelocity
                });
            }
            return result;
        }

        private Preset FindPreset(int bank, int program)
        {
            Preset fallback = null;
            foreach (var p in soundFont.Presets)
            {
                if (p.PatchNumber == program)
                {
                    if (p.Bank == bank) return p;
                    fallback ??= p; // same program, any bank
                }
            }
            return fallback;
        }

        private static float[] ConvertSampleData(SoundFont.SoundFont soundFont)
        {
            byte[] data = soundFont.SampleData;
            byte[] low = soundFont.SampleData24;
            int count = data.Length / 2;
            var samples = new float[count];

            if (low != null && low.Length >= count)
            {
                // 24-bit: combine the 16-bit high word with the 8-bit low byte
                const float scale = 1f / 8388608f; // 2^23
                for (int i = 0; i < count; i++)
                {
                    short high = (short)(data[i * 2] | (data[i * 2 + 1] << 8));
                    int value = (high << 8) | low[i];
                    samples[i] = value * scale;
                }
            }
            else
            {
                const float scale = 1f / 32768f;
                for (int i = 0; i < count; i++)
                {
                    short value = (short)(data[i * 2] | (data[i * 2 + 1] << 8));
                    samples[i] = value * scale;
                }
            }
            return samples;
        }
    }
}
