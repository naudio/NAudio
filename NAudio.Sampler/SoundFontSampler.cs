using System;
using System.Collections.Generic;
using NAudio.Effects;
using NAudio.Midi;
using NAudio.SoundFont;
using NAudio.Wave;

namespace NAudio.Sampler
{
    /// <summary>
    /// A polyphonic software sampler that plays a <see cref="SoundFont"/> in
    /// response to MIDI note and controller events, rendering 32-bit float
    /// stereo audio through the standard NAudio <see cref="ISampleProvider"/>
    /// pull model. Drive it live (feed live MIDI-in events) or offline (feed a
    /// <see cref="MidiFile"/>'s events on a schedule and render to a WAV).
    ///
    /// This is the v1 voice engine: pitch, looping, DAHDSR amplitude envelopes,
    /// a static per-voice low-pass filter, velocity/attenuation gain, pan, voice
    /// stealing and exclusive (choke) groups, plus basic channel state
    /// (program/bank select, pitch-bend, sustain pedal, volume/expression).
    /// The SoundFont modulator engine (mod-wheel→filter, velocity→filter, the
    /// default modulators, LFOs and the mod envelope) is a later step.
    /// </summary>
    public sealed class SoundFontSampler : ISampleProvider
    {
        private readonly SoundFont.SoundFont soundFont;
        private readonly float[] samplePool;
        private readonly int sampleRate;
        private readonly SamplerVoice[] voices;
        private readonly MidiChannelState[] channels;
        private readonly float[] mixBuffer;
        private readonly SendBus reverbBus;
        private readonly SendBus chorusBus;

        // preset lookup keyed by (bank<<16 | program); regions resolved lazily
        private readonly Dictionary<int, IReadOnlyList<SoundFontRegion>> regionCache = new();

        // neutral region (sample + generators + modulators) per SF2 region, built on first use
        private readonly Dictionary<SoundFontRegion, SamplerRegion> samplerRegionCache = new();

        private long startOrder;
        private float masterGain = 1f;

        private const int MaxFramesPerBlock = 1024;

        /// <summary>
        /// Creates a sampler for the given SoundFont.
        /// </summary>
        /// <param name="soundFont">The loaded SoundFont to play.</param>
        /// <param name="sampleRate">Output sample rate in Hz (default 44100).</param>
        /// <param name="maxVoices">Maximum simultaneous voices (default 64).</param>
        public SoundFontSampler(SoundFont.SoundFont soundFont, int sampleRate = 44100, int maxVoices = 64)
        {
            this.soundFont = soundFont ?? throw new ArgumentNullException(nameof(soundFont));
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
            if (maxVoices < 1) throw new ArgumentOutOfRangeException(nameof(maxVoices));

            this.sampleRate = sampleRate;
            samplePool = ConvertSampleData(soundFont);

            voices = new SamplerVoice[maxVoices];
            for (int i = 0; i < voices.Length; i++)
                voices[i] = new SamplerVoice(sampleRate);

            channels = new MidiChannelState[16];
            for (int i = 0; i < channels.Length; i++)
                channels[i] = new MidiChannelState();
            // GM: channel 10 (index 9) is the percussion bank
            channels[9].Bank = 128;

            mixBuffer = new float[MaxFramesPerBlock * 2];
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2);

            // shared reverb/chorus send buses: SF2 reverbEffectsSend/chorusEffectsSend
            // (and the CC91/CC93 default modulators) feed these per voice
            Reverb = new ReverbEffect();
            Chorus = new ChorusEffect();
            reverbBus = new SendBus(Reverb);
            chorusBus = new SendBus(Chorus);
            reverbBus.Configure(WaveFormat, MaxFramesPerBlock);
            chorusBus.Configure(WaveFormat, MaxFramesPerBlock);
        }

        /// <summary>The output format: 32-bit float stereo at the configured sample rate.</summary>
        public WaveFormat WaveFormat { get; }

        /// <summary>
        /// The shared reverb effect that voices feed through their reverb send
        /// (SF2 <c>reverbEffectsSend</c> / CC91). Tweak its parameters or
        /// <see cref="AudioEffect.Bypass"/> it to taste.
        /// </summary>
        public ReverbEffect Reverb { get; }

        /// <summary>
        /// The shared chorus effect that voices feed through their chorus send
        /// (SF2 <c>chorusEffectsSend</c> / CC93).
        /// </summary>
        public ChorusEffect Chorus { get; }

        /// <summary>Master output gain applied after mixing all voices. Default 1.</summary>
        public float MasterGain
        {
            get => masterGain;
            set => masterGain = value;
        }

        /// <summary>The number of voices currently sounding.</summary>
        public int ActiveVoiceCount
        {
            get
            {
                int n = 0;
                foreach (var v in voices) if (v.IsActive) n++;
                return n;
            }
        }

        /// <summary>
        /// Dispatches a MIDI event to the sampler (note on/off, control change,
        /// pitch-bend, program change). Events on unsupported message types are
        /// ignored.
        /// </summary>
        public void ProcessMidiEvent(MidiEvent midiEvent)
        {
            if (midiEvent == null) return;
            int channel = midiEvent.Channel - 1; // NAudio channels are 1-based
            if ((uint)channel >= 16) return;

            switch (midiEvent)
            {
                case NoteOnEvent on when on.Velocity > 0:
                    NoteOn(channel, on.NoteNumber, on.Velocity);
                    break;
                case NoteOnEvent on: // velocity 0 == note off
                    NoteOff(channel, on.NoteNumber);
                    break;
                case NoteEvent off when off.CommandCode == MidiCommandCode.NoteOff:
                    NoteOff(channel, off.NoteNumber);
                    break;
                case ControlChangeEvent cc:
                    ControlChange(channel, cc.Controller, cc.ControllerValue);
                    break;
                case PitchWheelChangeEvent pitch:
                    channels[channel].PitchBend = pitch.Pitch;
                    break;
                case ChannelAfterTouchEvent aftertouch:
                    channels[channel].ChannelPressure = aftertouch.AfterTouchPressure;
                    break;
                case PatchChangeEvent patch:
                    channels[channel].Program = patch.Patch;
                    break;
            }
        }

        /// <summary>
        /// Starts a note on a channel at a given velocity.
        /// </summary>
        public void NoteOn(int channel, int note, int velocity)
        {
            if ((uint)channel >= 16) return;
            if (velocity <= 0) { NoteOff(channel, note); return; }

            var state = channels[channel];
            var regions = GetRegions(state.Bank, state.Program);
            if (regions == null) return;

            foreach (var region in regions)
            {
                if (!region.Matches(note, velocity)) continue;

                // exclusive class: choke other voices in the same class first
                int exclusiveClass = region.Generators.ExclusiveClass;
                if (exclusiveClass != 0) ChokeExclusiveClass(exclusiveClass, channel);

                var voice = AcquireVoice();
                voice.Start(GetSamplerRegion(region), state, channel, note, velocity, startOrder++);
            }
        }

        // projects a resolved SoundFont region onto the format-neutral region the
        // voice plays: the shared sample pool sliced by the sample header, the
        // generators as-is, and the combined (default + file) modulator list
        private SamplerRegion GetSamplerRegion(SoundFontRegion region)
        {
            if (samplerRegionCache.TryGetValue(region, out var samplerRegion)) return samplerRegion;

            var sh = region.Sample;
            samplerRegion = new SamplerRegion
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
                LoKey = region.LowKey,
                HiKey = region.HighKey,
                LoVelocity = region.LowVelocity,
                HiVelocity = region.HighVelocity
            };
            samplerRegionCache[region] = samplerRegion;
            return samplerRegion;
        }

        /// <summary>
        /// Releases a note on a channel. With the sustain pedal down the note is
        /// remembered and only released when the pedal rises.
        /// </summary>
        public void NoteOff(int channel, int note)
        {
            if ((uint)channel >= 16) return;

            if (channels[channel].SustainPedal)
            {
                sustainedNotes.Add((channel, note)); // released when the pedal lifts
                return;
            }

            foreach (var v in voices)
                if (v.IsActive && v.IsHeld && v.Channel == channel && v.Note == note)
                    v.Release();
        }

        /// <summary>Stops every sounding voice immediately (with a short fade).</summary>
        public void AllSoundOff()
        {
            foreach (var v in voices) v.Choke();
        }

        /// <summary>Releases all held notes on every channel.</summary>
        public void AllNotesOff()
        {
            foreach (var v in voices) if (v.IsActive && v.IsHeld) v.Release();
        }

        /// <inheritdoc />
        public int Read(Span<float> buffer)
        {
            int count = buffer.Length;
            // stereo: count is a multiple of 2; render in blocks
            int framesRemaining = count / 2;
            int written = 0;

            while (framesRemaining > 0)
            {
                int frames = Math.Min(framesRemaining, MaxFramesPerBlock);
                var block = mixBuffer.AsSpan(0, frames * 2);
                block.Clear();
                var reverbSend = reverbBus.PrepareSend(frames);
                var chorusSend = chorusBus.PrepareSend(frames);

                foreach (var v in voices)
                {
                    if (!v.IsActive) continue;
                    v.Mix(block, reverbSend, chorusSend, frames, channels[v.Channel]);
                }

                // run the shared effects and return the wet signal into the mix
                reverbBus.ProcessReturn(block, frames);
                chorusBus.ProcessReturn(block, frames);

                float g = masterGain;
                for (int i = 0; i < frames * 2; i++)
                    buffer[written + i] = block[i] * g;

                written += frames * 2;
                framesRemaining -= frames;
            }

            // a sampler is an instrument: it always returns a full buffer of
            // (mostly silent) audio rather than signalling end-of-stream
            return count;
        }

        private void ControlChange(int channel, MidiController controller, int value)
        {
            var state = channels[channel];

            // store every controller so the modulator engine can read it as a
            // source (CC1 mod wheel, CC7 volume, CC10 pan, CC11 expression,
            // CC91/CC93 sends, etc.)
            state.SetController((int)controller, value);

            switch (controller)
            {
                case MidiController.Sustain:
                    bool down = value >= 64;
                    state.SustainPedal = down;
                    if (!down) ReleaseSustainedNotes(channel);
                    break;
                case MidiController.AllNotesOff:
                    AllNotesOff();
                    break;
                case (MidiController)120: // All Sound Off (not in the MidiController enum)
                    AllSoundOff();
                    break;
                case MidiController.ResetAllControllers:
                    state.ResetControllers();
                    break;
                case MidiController.BankSelect:
                    state.Bank = (state.Bank & 0x7F) | (value << 7);
                    break;
                case MidiController.BankSelectLsb:
                    state.Bank = (state.Bank & ~0x7F) | value;
                    break;
            }
        }

        private void ReleaseSustainedNotes(int channel)
        {
            // pedal up: release every voice whose key was lifted while the pedal
            // was down (tracked in sustainedNotes)
            foreach (var v in voices)
                if (v.IsActive && v.IsHeld && v.Channel == channel &&
                    sustainedNotes.Contains((channel, v.Note)))
                    v.Release();
            sustainedNotes.RemoveWhere(k => k.channel == channel);
        }

        // (channel, note) keys lifted while the sustain pedal was held down
        private readonly HashSet<(int channel, int note)> sustainedNotes = new();

        private void ChokeExclusiveClass(int exclusiveClass, int channel)
        {
            foreach (var v in voices)
                if (v.IsActive && v.ExclusiveClass == exclusiveClass && v.Channel == channel)
                    v.Choke();
        }

        private SamplerVoice AcquireVoice()
        {
            // free voice first
            foreach (var v in voices)
                if (!v.IsActive) return v;

            // steal: prefer a released voice, else the quietest, breaking ties by age
            SamplerVoice best = voices[0];
            foreach (var v in voices)
            {
                bool vReleased = !v.IsHeld;
                bool bestReleased = !best.IsHeld;
                if (vReleased != bestReleased)
                {
                    if (vReleased) best = v;
                    continue;
                }
                if (v.Level < best.Level ||
                    (v.Level == best.Level && v.StartOrder < best.StartOrder))
                    best = v;
            }
            return best;
        }

        private IReadOnlyList<SoundFontRegion> GetRegions(int bank, int program)
        {
            int key = (bank << 16) | program;
            if (regionCache.TryGetValue(key, out var cached)) return cached;

            var preset = FindPreset(bank, program);
            var regions = preset?.ResolveRegions();
            regionCache[key] = regions;
            return regions;
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
