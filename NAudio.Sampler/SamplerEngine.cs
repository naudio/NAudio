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
    /// <remarks>
    /// The engine is single-threaded by design: <see cref="Read"/> and every MIDI
    /// entry point (<see cref="ProcessMidiEvent"/>, <see cref="NoteOn"/>,
    /// <see cref="NoteOff"/>, …) must be called from one thread — normally the
    /// audio thread. To drive it from asynchronous sources (a MIDI input
    /// callback, a UI), wrap it in <see cref="Midi.LiveMidiInstrument"/>, which
    /// queues events lock-free and applies them on the audio thread.
    ///
    /// Subclassing is deliberately internal for v1: the abstract region supply is
    /// <c>private protected</c> and the format-neutral region model is internal,
    /// so new instrument formats are added inside NAudio.Sampler. Widening this
    /// later is non-breaking; see <c>Docs/Architecture/SamplerDesign.md</c> §11.
    /// </remarks>
    public abstract class SamplerEngine : IMidiInstrument
    {
        /// <summary>Maximum frames rendered per internal block.</summary>
        protected const int MaxFramesPerBlock = 1024;

        private readonly SamplerVoice[] voices;
        private readonly float[] mixBuffer;
        private readonly SendBus reverbBus;
        private readonly SendBus chorusBus;
        // notes released while the sustain pedal was down, parked until pedal-up;
        // the note-on velocity and frame are kept so the pedal-up release can fire
        // release-triggered regions with the right rt_decay held-time
        private readonly Dictionary<(int channel, int note), (int Velocity, long OnFrame)> sustainedNotes = new();
        // reusable scratch for releasing parked notes without allocating per pedal-up
        private readonly List<KeyValuePair<(int channel, int note), (int Velocity, long OnFrame)>> sustainedScratch = new();
        // keys currently held down per channel (note -> note-on velocity and the
        // frame it started), for legato/first triggers and release rt_decay
        private readonly Dictionary<int, (int Velocity, long OnFrame)>[] heldNotes;
        // last keyswitch key pressed per channel (-1 = none yet)
        private readonly int[] lastSwitch;
        private readonly Random rng = new();
        private long startOrder;
        private long framesRendered; // a sample clock for rt_decay held-time
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
            heldNotes = new Dictionary<int, (int, long)>[16];
            lastSwitch = new int[16];
            for (int i = 0; i < Channels.Length; i++)
            {
                Channels[i] = new MidiChannelState();
                heldNotes[i] = new Dictionary<int, (int, long)>();
                lastSwitch[i] = -1;
            }

            mixBuffer = new float[MaxFramesPerBlock * 2];
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2);

            Reverb = new ReverbEffect();
            Chorus = new ChorusEffect();
            // The buses carry only the send portion of each voice, so the effects
            // must return fully wet: their default part-dry Mix would add the raw
            // send signal back into the output as a level error (and cost an
            // extra dry/wet blend pass per block).
            Reverb.Mix = 1f;
            Chorus.Mix = 1f;
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
        /// Copies each active voice's current source-sample read position into
        /// <paramref name="destination"/> (up to its length), returning the count
        /// written. For UI playback indicators — allocation-free and cheap; safe to
        /// call from the UI thread (a momentarily stale read is harmless).
        /// </summary>
        public int GetActivePlaybackPositions(double[] destination)
        {
            if (destination == null) return 0;
            int n = 0;
            foreach (var v in voices)
            {
                if (n >= destination.Length) break;
                if (v.IsActive) destination[n++] = v.SamplePosition;
            }
            return n;
        }

        /// <summary>
        /// The candidate regions a note on the given channel might play; the engine
        /// filters them by key/velocity. Return null for "no instrument".
        /// </summary>
        private protected abstract IReadOnlyList<SamplerRegion> GetRegionsForNoteOn(int channel, MidiChannelState state);

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
            var regions = GetRegionsForNoteOn(channel, state);

            // "first" plays only when no other key is held; "legato" only when one is
            bool legato = heldNotes[channel].Count > 0;
            double random = rng.NextDouble(); // one draw per note-on, so layers select consistently

            // Re-striking a sounding key supersedes its previous voices: release
            // them into their tails and forget any pedal-parked note-off, so a
            // later pedal-up can't kill the new note and pedalled repeats don't
            // accumulate held voices without bound.
            sustainedNotes.Remove((channel, note));
            foreach (var v in voices)
                if (v.IsActive && v.IsHeld && !v.IgnoreNoteOff && v.Channel == channel && v.Note == note)
                    v.Release();

            if (regions != null)
            {
                // voices started by this note-on are exempt from its own chokes: an
                // exclusive class terminates *other* notes, not sibling layers of
                // the note being started (e.g. stereo-linked drum zones)
                long dispatch = startOrder;
                foreach (var region in regions)
                {
                    if (region.IsCcTriggered) continue; // CC-triggered, not key-triggered
                    if (!region.Matches(note, velocity)) continue;
                    if (!PlaysOnNoteOn(region.Trigger, legato)) continue;
                    if (!KeyswitchActive(region, channel)) continue;
                    if (!region.PassesCcGates(state)) continue;
                    if (!region.PassesRandom(random)) continue;
                    if (!region.PassesSequence()) continue; // advances the round-robin counter

                    StartVoice(region, state, channel, note, velocity, dispatch);
                }
            }

            heldNotes[channel][note] = (velocity, framesRendered); // key down (for legato / release rt_decay)
        }

        // gates on the key/velocity crossfade gain (silent layers don't spawn a
        // voice), chokes the off_by group (sparing voices started by the same
        // dispatch), and starts a voice scaled by the crossfade and any extra
        // (e.g. release rt_decay) gain
        private void StartVoice(SamplerRegion region, MidiChannelState state, int channel, int note, int velocity, long dispatchOrder, float extraGain = 1f)
        {
            float gain = region.CrossfadeGain(note, velocity) * extraGain;
            if (gain <= 0f) return;

            if (region.OffByGroup != 0) ChokeGroup(region.OffByGroup, channel, dispatchOrder);

            var voice = AcquireVoice();
            voice.Start(region, state, channel, note, velocity, startOrder++, gain);
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

            bool tracked = heldNotes[channel].TryGetValue(note, out var held);
            if (tracked) heldNotes[channel].Remove(note);

            if (Channels[channel].SustainPedal)
            {
                // park the note-off until pedal-up, keeping the note-on data so the
                // eventual release can fire release triggers with the right held-time
                sustainedNotes[(channel, note)] = tracked ? held : (64, framesRendered);
                return;
            }

            // release the sounding voices for this key first...
            foreach (var v in voices)
                if (v.IsActive && v.IsHeld && !v.IgnoreNoteOff && v.Channel == channel && v.Note == note)
                    v.Release();

            // ...then fire release-triggered regions (on the same note), so this
            // note-off doesn't immediately release the release voices it just started.
            // Attenuate them by how long the key was held (rt_decay).
            if (tracked)
            {
                double heldSeconds = (framesRendered - held.OnFrame) / (double)WaveFormat.SampleRate;
                FireReleaseTriggers(channel, note, held.Velocity, heldSeconds);
            }
        }

        private void FireReleaseTriggers(int channel, int note, int velocity, double heldSeconds)
        {
            var state = Channels[channel];
            var regions = GetRegionsForNoteOn(channel, state);
            if (regions == null) return;

            long dispatch = startOrder; // this dispatch's voices are exempt from its chokes
            foreach (var region in regions)
            {
                if (region.Trigger != SamplerTrigger.Release) continue;
                if (!region.Matches(note, velocity)) continue;

                StartVoice(region, state, channel, note, velocity, dispatch, region.ReleaseDecayGain(heldSeconds));
            }
        }

        // starts any CC-triggered regions whose on_loccN/on_hiccN window the
        // controller has just risen into, playing them at the region's root key
        private void FireOnCcTriggers(int channel, MidiChannelState state, int cc, int oldValue, int newValue)
        {
            var regions = GetRegionsForNoteOn(channel, state);
            if (regions == null) return;

            long dispatch = startOrder; // this dispatch's voices are exempt from its chokes
            foreach (var region in regions)
            {
                if (!region.IsCcTriggered) continue;
                if (!region.TriggeredByCcChange(cc, oldValue, newValue)) continue;

                int note = region.Sample.RootKey;
                StartVoice(region, state, channel, note, Math.Clamp(newValue, 1, 127), dispatch);
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
                framesRendered += frames; // sample clock for rt_decay held-time
            }

            // a sampler is an instrument: always returns a full (mostly silent) buffer
            return count;
        }

        private void ControlChange(int channel, MidiController controller, int value)
        {
            var state = Channels[channel];

            // store every controller so the modulator engine can read it as a source,
            // remembering the previous value to detect on_cc trigger edges
            int cc = (int)controller;
            int previous = state.Controller(cc);
            state.SetController(cc, value);
            if (value != previous) FireOnCcTriggers(channel, state, cc, previous, value);

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
                    // SF2 wBank (0-127) corresponds to the bank-select MSB; the LSB
                    // is kept separately for GS/XG-style variation selection
                    state.Bank = value;
                    break;
                case MidiController.BankSelectLsb:
                    state.BankLsb = value;
                    break;
            }
        }

        private void ReleaseSustainedNotes(int channel)
        {
            sustainedScratch.Clear();
            foreach (var entry in sustainedNotes)
                if (entry.Key.channel == channel)
                    sustainedScratch.Add(entry);

            foreach (var entry in sustainedScratch)
            {
                sustainedNotes.Remove(entry.Key);
                int note = entry.Key.note;

                foreach (var v in voices)
                    if (v.IsActive && v.IsHeld && !v.IgnoreNoteOff && v.Channel == channel && v.Note == note)
                        v.Release();

                // the pedal, not the key, ends these notes — so release triggers
                // (and their rt_decay held-time) fire now, like a damper falling
                double heldSeconds = (framesRendered - entry.Value.OnFrame) / (double)WaveFormat.SampleRate;
                FireReleaseTriggers(channel, note, entry.Value.Velocity, heldSeconds);
            }
        }

        // silence sounding voices belonging to the given choke group on a channel,
        // sparing voices started at or after startedSince (a note-on must not choke
        // the sibling layers it is itself starting)
        private void ChokeGroup(int group, int channel, long startedSince)
        {
            foreach (var v in voices)
                if (v.IsActive && v.Group == group && v.Channel == channel && v.StartOrder < startedSince)
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
