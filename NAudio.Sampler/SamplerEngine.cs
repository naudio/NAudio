using System;
using System.Collections.Generic;
using NAudio.Effects;
using NAudio.Midi;
using NAudio.Wave;

namespace NAudio.Sampler
{
    /// <summary>
    /// The shared polyphonic sampler engine: a pool of <see cref="SamplerVoice"/>s
    /// driven by MIDI, rendering 32-bit float stereo through the
    /// <see cref="ISampleProvider"/> pull model, with voice stealing, exclusive
    /// (choke) groups, sustain-pedal hold, per-channel controller state, and
    /// shared reverb/chorus send buses. Format-specific subclasses
    /// (<see cref="SoundFontSampler"/>, <see cref="SfzSampler"/>) only supply the
    /// regions a note should play; everything else lives here so both formats run
    /// through one engine.
    /// </summary>
    public abstract class SamplerEngine : ISampleProvider
    {
        /// <summary>Maximum frames rendered per internal block.</summary>
        protected const int MaxFramesPerBlock = 1024;

        private readonly SamplerVoice[] voices;
        private readonly float[] mixBuffer;
        private readonly SendBus reverbBus;
        private readonly SendBus chorusBus;
        private readonly HashSet<(int channel, int note)> sustainedNotes = new();
        private long startOrder;
        private float masterGain = 1f;

        /// <summary>The live controller state for each of the 16 MIDI channels.</summary>
        private protected MidiChannelState[] Channels { get; }

        /// <summary>Creates the engine with a voice pool and the shared send buses.</summary>
        protected SamplerEngine(int sampleRate, int maxVoices)
        {
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
            if (maxVoices < 1) throw new ArgumentOutOfRangeException(nameof(maxVoices));

            voices = new SamplerVoice[maxVoices];
            for (int i = 0; i < voices.Length; i++)
                voices[i] = new SamplerVoice(sampleRate);

            Channels = new MidiChannelState[16];
            for (int i = 0; i < Channels.Length; i++)
                Channels[i] = new MidiChannelState();

            mixBuffer = new float[MaxFramesPerBlock * 2];
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2);

            Reverb = new ReverbEffect();
            Chorus = new ChorusEffect();
            reverbBus = new SendBus(Reverb);
            chorusBus = new SendBus(Chorus);
            reverbBus.Configure(WaveFormat, MaxFramesPerBlock);
            chorusBus.Configure(WaveFormat, MaxFramesPerBlock);
        }

        /// <summary>The output format: 32-bit float stereo at the configured sample rate.</summary>
        public WaveFormat WaveFormat { get; }

        /// <summary>The shared reverb effect voices feed through their reverb send.</summary>
        public ReverbEffect Reverb { get; }

        /// <summary>The shared chorus effect voices feed through their chorus send.</summary>
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
        /// The candidate regions a note on the given channel might play; the engine
        /// filters them by key/velocity. Return null for "no instrument".
        /// </summary>
        private protected abstract IReadOnlyList<SamplerRegion> GetRegionsForNoteOn(MidiChannelState channel);

        /// <summary>
        /// Dispatches a MIDI event (note on/off, control change, pitch-bend,
        /// channel pressure, program change). Unsupported messages are ignored.
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
                    Channels[channel].PitchBend = pitch.Pitch;
                    break;
                case ChannelAfterTouchEvent aftertouch:
                    Channels[channel].ChannelPressure = aftertouch.AfterTouchPressure;
                    break;
                case PatchChangeEvent patch:
                    Channels[channel].Program = patch.Patch;
                    break;
            }
        }

        /// <summary>Starts a note on a channel at a given velocity.</summary>
        public void NoteOn(int channel, int note, int velocity)
        {
            if ((uint)channel >= 16) return;
            if (velocity <= 0) { NoteOff(channel, note); return; }

            var state = Channels[channel];
            var regions = GetRegionsForNoteOn(state);
            if (regions == null) return;

            foreach (var region in regions)
            {
                if (!region.Matches(note, velocity)) continue;

                if (region.ExclusiveClass != 0) ChokeExclusiveClass(region.ExclusiveClass, channel);

                var voice = AcquireVoice();
                voice.Start(region, state, channel, note, velocity, startOrder++);
            }
        }

        /// <summary>
        /// Releases a note on a channel. With the sustain pedal down the note is
        /// remembered and only released when the pedal rises.
        /// </summary>
        public void NoteOff(int channel, int note)
        {
            if ((uint)channel >= 16) return;

            if (Channels[channel].SustainPedal)
            {
                sustainedNotes.Add((channel, note));
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
            int framesRemaining = count / 2; // stereo
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
                    v.Mix(block, reverbSend, chorusSend, frames, Channels[v.Channel]);
                }

                reverbBus.ProcessReturn(block, frames);
                chorusBus.ProcessReturn(block, frames);

                float g = masterGain;
                for (int i = 0; i < frames * 2; i++)
                    buffer[written + i] = block[i] * g;

                written += frames * 2;
                framesRemaining -= frames;
            }

            // a sampler is an instrument: always returns a full (mostly silent) buffer
            return count;
        }

        private void ControlChange(int channel, MidiController controller, int value)
        {
            var state = Channels[channel];

            // store every controller so the modulator engine can read it as a source
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
            foreach (var v in voices)
                if (v.IsActive && v.IsHeld && v.Channel == channel &&
                    sustainedNotes.Contains((channel, v.Note)))
                    v.Release();
            sustainedNotes.RemoveWhere(k => k.channel == channel);
        }

        private void ChokeExclusiveClass(int exclusiveClass, int channel)
        {
            foreach (var v in voices)
                if (v.IsActive && v.ExclusiveClass == exclusiveClass && v.Channel == channel)
                    v.Choke();
        }

        private SamplerVoice AcquireVoice()
        {
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
    }
}
