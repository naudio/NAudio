using System;
using System.Collections.Concurrent;
using NAudio.Wave;

namespace NAudio.Midi
{
    /// <summary>
    /// Plays an <see cref="IMidiInstrument"/> from a live MIDI source (a hardware
    /// keyboard, an on-screen keyboard, etc.). Unlike <see cref="SequencedMidiPlayer"/>,
    /// which reads events from a timeline on the audio thread, events here arrive
    /// asynchronously from other threads (a MIDI input callback, the UI thread, …).
    /// </summary>
    /// <remarks>
    /// Incoming events are placed on a lock-free queue by <see cref="Send"/> and
    /// drained on the audio thread at the start of each <see cref="Read"/>, so the
    /// (single-threaded) wrapped instrument is only ever touched from one thread.
    /// Events therefore take effect at the next block boundary; the resulting
    /// latency is just the output buffer size, which is the expected behaviour for
    /// a live, pull-driven instrument.
    ///
    /// The wrapper is itself an <see cref="IMidiInstrument"/> whose
    /// <see cref="ProcessMidiEvent"/> is the thread-safe, deferred <see cref="Send"/>,
    /// so hosts can treat live-bridged and directly-driven instruments uniformly.
    /// </remarks>
    public sealed class LiveMidiInstrument : IMidiInstrument
    {
        private readonly IMidiInstrument instrument;
        private readonly ConcurrentQueue<MidiEvent> queue = new();

        /// <summary>Wraps the given instrument so it can be driven from live MIDI input.</summary>
        public LiveMidiInstrument(IMidiInstrument instrument)
        {
            this.instrument = instrument ?? throw new ArgumentNullException(nameof(instrument));
        }

        /// <summary>The wrapped instrument.</summary>
        public IMidiInstrument Instrument => instrument;

        /// <inheritdoc/>
        public WaveFormat WaveFormat => instrument.WaveFormat;

        /// <summary>
        /// Queues a MIDI event for the instrument. Safe to call from any thread;
        /// the event is applied on the audio thread at the start of the next block.
        /// Null events are ignored.
        /// </summary>
        public void Send(MidiEvent midiEvent)
        {
            if (midiEvent != null) queue.Enqueue(midiEvent);
        }

        /// <inheritdoc/>
        public void ProcessMidiEvent(MidiEvent midiEvent) => Send(midiEvent);

        /// <summary>
        /// Queues an "all sound off" (CC120) for every channel — the panic button.
        /// Safe to call from any thread.
        /// </summary>
        public void AllSoundOff()
        {
            for (int channel = 1; channel <= 16; channel++)
                Send(new ControlChangeEvent(0, channel, (MidiController)120, 0));
        }

        /// <summary>Queues a note-on. <paramref name="channel"/> is zero-based.</summary>
        public void NoteOn(int channel, int note, int velocity) =>
            Send(new NoteOnEvent(0, channel + 1, note, velocity, 0));

        /// <summary>Queues a note-off. <paramref name="channel"/> is zero-based.</summary>
        public void NoteOff(int channel, int note) =>
            Send(new NoteEvent(0, channel + 1, MidiCommandCode.NoteOff, note, 0));

        /// <inheritdoc/>
        public int Read(Span<float> buffer)
        {
            while (queue.TryDequeue(out var midiEvent))
                instrument.ProcessMidiEvent(midiEvent);
            return instrument.Read(buffer);
        }
    }
}
