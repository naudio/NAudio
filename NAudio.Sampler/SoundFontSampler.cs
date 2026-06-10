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
            samplePool = soundFont.ReadSampleDataFloat();
            PrewarmPresets();
        }

        /// <summary>
        /// Resolves and projects every preset in the font at construction, so the
        /// first note-on per program doesn't run a burst of resolution/projection
        /// work (and its allocations) inside <see cref="SamplerEngine.Read"/> —
        /// which matters for multi-timbral playback. <see cref="GetRegionsForNoteOn"/>
        /// stays lazy-capable for lookups the prewarm cannot anticipate (missing
        /// programs resolving through the bank-fallback rules).
        /// </summary>
        private void PrewarmPresets()
        {
            foreach (var preset in soundFont.Presets)
            {
                if (preset == null) continue;
                // a bank-128 preset is only reachable through the forced-percussion
                // path (CC0 is 7-bit, so bank 128 is never selected for a melodic
                // lookup); every other preset is reachable at its own (bank, program)
                int key = preset.Bank == PercussionBank
                    ? (1 << 24) | (PercussionBank << 16) | preset.PatchNumber
                    : (preset.Bank << 16) | preset.PatchNumber;
                // first preset with a given (bank, program) wins, matching the
                // FindPreset/FindPercussionPreset iteration order
                if (regionCache.ContainsKey(key)) continue;

                var resolved = preset.ResolveRegions();
                regionCache[key] = resolved == null ? null : Project(resolved);
            }
        }

        /// <summary>The number of cached preset projections (for tests).</summary>
        internal int CachedPresetCount => regionCache.Count;

        /// <summary>The SoundFont bank holding GM percussion kits.</summary>
        public const int PercussionBank = 128;

        // GS marks a rhythm part by selecting bank MSB 120 on any channel;
        // XG uses MSB 127 for its drum-kit banks
        private const int GsRhythmBankMsb = 120;
        private const int XgRhythmBankMsb = 127;

        /// <summary>
        /// The MIDI channel (0-based) treated as the GM percussion channel: its
        /// notes always resolve against the percussion bank (<see cref="PercussionBank"/> = 128),
        /// regardless of any bank-select messages — note numbers pick the drum, not
        /// a pitch. Defaults to 9 (MIDI channel 10). Set to -1 to disable, e.g. for
        /// a non-GM SoundFont with no percussion bank.
        /// </summary>
        public int PercussionChannel { get; set; } = 9;

        /// <summary>
        /// Whether a channel whose selected bank MSB (CC0) is 120 or 127 resolves
        /// its notes against the percussion bank, exactly like the forced
        /// percussion channel. Roland GS selects rhythm parts with bank MSB 120
        /// on any channel and Yamaha XG uses MSB 127 for its drum kits, so
        /// honouring those two values lets GS/XG-authored MIDI files play drums
        /// on channels other than 10. Default true. This is a heuristic on the
        /// bank number alone: full GS/XG mode detection via SysEx (GS Reset /
        /// XG System On, part-mode messages) remains unsupported. Normal
        /// variation banks (e.g. CC0 = 8) are unaffected.
        /// </summary>
        public bool TreatGsXgDrumBanksAsPercussion { get; set; } = true;

        /// <inheritdoc />
        private protected override IReadOnlyList<SamplerRegion> GetRegionsForNoteOn(int channel, MidiChannelState state)
        {
            // GM channel 10 is always percussion: force bank 128 so a stray
            // bank-select on the drum track can't drop it onto a melodic preset.
            // A GS (CC0 = 120) or XG (CC0 = 127) rhythm-bank selection makes any
            // other channel percussion too while the heuristic is enabled —
            // resolving through the same forced-percussion path (and cache key
            // shape), so the prewarm and cache-collision protections hold.
            bool percussion = channel == PercussionChannel ||
                (TreatGsXgDrumBanksAsPercussion &&
                 (state.Bank == GsRhythmBankMsb || state.Bank == XgRhythmBankMsb));
            int bank = percussion ? PercussionBank : state.Bank;

            // the forced-percussion path resolves differently from a melodic lookup,
            // so it gets its own cache slot even if the bank number coincides
            int key = (percussion ? 1 << 24 : 0) | (bank << 16) | state.Program;
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
                    // exclusiveClass is a self-choking group (a note silences others
                    // in its class); OffMode stays Fast and Polyphony 0 (the
                    // defaults) — SF2 has neither concept
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

        // a melodic program in a missing bank falls back to bank 0 (GS-style
        // capital tone), else any other melodic bank — never to a percussion kit
        private Preset FindPreset(int bank, int program)
        {
            Preset bankZero = null;
            Preset fallback = null;
            foreach (var p in soundFont.Presets)
            {
                if (p.PatchNumber != program) continue;
                if (p.Bank == bank) return p;
                if (p.Bank == 0) bankZero ??= p;
                else if (p.Bank != PercussionBank) fallback ??= p;
            }
            return bankZero ?? fallback;
        }

    }
}
