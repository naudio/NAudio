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
        // keys currently held down per channel (note -> note-on velocity), for
        // legato/first triggers and release-trigger velocity
        private readonly Dictionary<int, int>[] heldNotes;
        // last keyswitch key pressed per channel (-1 = none yet)
        private readonly int[] lastSwitch;
        private readonly Random rng = new();
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
            heldNotes = new Dictionary<int, int>[16];
            lastSwitch = new int[16];
            for (int i = 0; i < Channels.Length; i++)
            {
                Channels[i] = new MidiChannelState();
                heldNotes[i] = new Dictionary<int, int>();
                lastSwitch[i] = -1;
            }

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

            // a press in the keyswitch range selects the active articulation and makes no sound
            if (IsKeyswitch(note)) { lastSwitch[channel] = note; return; }

            var state = Channels[channel];
            var regions = GetRegionsForNoteOn(state);

            // "first" plays only when no other key is held; "legato" only when one is
            bool legato = heldNotes[channel].Count > 0;
            double random = rng.NextDouble(); // one draw per note-on, so layers select consistently

            if (regions != null)
            {
                foreach (var region in regions)
                {
                    if (!region.Matches(note, velocity)) continue;
                    if (!PlaysOnNoteOn(region.Trigger, legato)) continue;
                    if (!KeyswitchActive(region, channel)) continue;
                    if (!region.PassesCcGates(state)) continue;
                    if (!region.PassesRandom(random)) continue;
                    if (!region.PassesSequence()) continue; // advances the round-robin counter

                    StartVoice(region, state, channel, note, velocity);
                }
            }

            heldNotes[channel][note] = velocity; // remember the key is down (for legato / release)
        }

        // gates on the key/velocity crossfade gain (silent layers don't spawn a
        // voice), chokes the off_by group, and starts a voice with that gain
        private void StartVoice(SamplerRegion region, MidiChannelState state, int channel, int note, int velocity)
        {
            float crossfadeGain = region.CrossfadeGain(note, velocity);
            if (crossfadeGain <= 0f) return;

            if (region.OffByGroup != 0) ChokeGroup(region.OffByGroup, channel);

            var voice = AcquireVoice();
            voice.Start(region, state, channel, note, velocity, startOrder++, crossfadeGain);
        }

        /// <summary>
        /// Whether a key acts as a keyswitch (selects an articulation) rather than
        /// sounding a note. Default false; SFZ overrides it with its keyswitch range.
        /// </summary>
        private protected virtual bool IsKeyswitch(int key) => false;

        private bool KeyswitchActive(SamplerRegion region, int channel)
        {
            if (region.KeyswitchLast < 0) return true; // region has no keyswitch requirement
            int last = lastSwitch[channel];
            if (last < 0) last = region.KeyswitchDefault; // nothing pressed yet -> the default
            return last == region.KeyswitchLast;
        }

        private static bool PlaysOnNoteOn(SamplerTrigger trigger, bool legato)
        {
            switch (trigger)
            {
                case SamplerTrigger.Attack: return true;
                case SamplerTrigger.First: return !legato;
                case SamplerTrigger.Legato: return legato;
                default: return false; // Release plays on note-off
            }
        }

        /// <summary>
        /// Releases a note on a channel. With the sustain pedal down the note is
        /// remembered and only released when the pedal rises.
        /// </summary>
        public void NoteOff(int channel, int note)
        {
            if ((uint)channel >= 16) return;

            // fire release-triggered regions for this key (using its note-on velocity)
            if (heldNotes[channel].TryGetValue(note, out int heldVelocity))
            {
                FireReleaseTriggers(channel, note, heldVelocity);
                heldNotes[channel].Remove(note);
            }

            if (Channels[channel].SustainPedal)
            {
                sustainedNotes.Add((channel, note));
                return;
            }

            foreach (var v in voices)
                if (v.IsActive && v.IsHeld && !v.IgnoreNoteOff && v.Channel == channel && v.Note == note)
                    v.Release();
        }

        private void FireReleaseTriggers(int channel, int note, int velocity)
        {
            var state = Channels[channel];
            var regions = GetRegionsForNoteOn(state);
            if (regions == null) return;

            foreach (var region in regions)
            {
                if (region.Trigger != SamplerTrigger.Release) continue;
                if (!region.Matches(note, velocity)) continue;

                StartVoice(region, state, channel, note, velocity);
            }
        }

        /// <summary>Stops every sounding voice immediately (with a short fade).</summary>
        public void AllSoundOff()
        {
            foreach (var v in voices) v.Choke();
        }

        /// <summary>Releases all held notes on every channel.</summary>
        public void AllNotesOff()
        {
            foreach (var v in voices) if (v.IsActive && v.IsHeld && !v.IgnoreNoteOff) v.Release();
            foreach (var held in heldNotes) held.Clear();
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
                if (v.IsActive && v.IsHeld && !v.IgnoreNoteOff && v.Channel == channel &&
                    sustainedNotes.Contains((channel, v.Note)))
                    v.Release();
            sustainedNotes.RemoveWhere(k => k.channel == channel);
        }

        // silence sounding voices belonging to the given choke group on a channel
        private void ChokeGroup(int group, int channel)
        {
            foreach (var v in voices)
                if (v.IsActive && v.Group == group && v.Channel == channel)
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
