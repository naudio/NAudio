using System;
using System.Collections.Concurrent;
using NAudio.Midi;
using NAudio.Wave;

namespace NAudio.Sampler
{
    /// <summary>
    /// Plays a <see cref="SamplerEngine"/> from a live MIDI source (a hardware
    /// keyboard, an on-screen keyboard, etc.), exposed as an
    /// <see cref="ISampleProvider"/>. Unlike <see cref="SequencedMidiInstrument"/>,
    /// which reads events from a timeline on the audio thread, events here arrive
    /// asynchronously from other threads (a MIDI input callback, the UI thread, …).
    /// </summary>
    /// <remarks>
    /// Incoming events are placed on a lock-free queue by <see cref="Send"/> and
    /// drained on the audio thread at the start of each <see cref="Read"/>, so the
    /// sampler is only ever touched from one thread. Events therefore take effect at
    /// the next block boundary; the resulting latency is just the output buffer size,
    /// which is the expected behaviour for a live, pull-driven instrument.
    /// </remarks>
    public sealed class LiveMidiInstrument : ISampleProvider
    {
        private readonly SamplerEngine sampler;
        private readonly ConcurrentQueue<MidiEvent> queue = new();

        /// <summary>Wraps the given sampler so it can be driven from live MIDI input.</summary>
        public LiveMidiInstrument(SamplerEngine sampler)
        {
            this.sampler = sampler ?? throw new ArgumentNullException(nameof(sampler));
        }

        /// <summary>The wrapped sampler engine.</summary>
        public SamplerEngine Sampler => sampler;

        /// <inheritdoc/>
        public WaveFormat WaveFormat => sampler.WaveFormat;

        /// <summary>
        /// Queues a MIDI event for the sampler. Safe to call from any thread; the
        /// event is applied on the audio thread at the start of the next block.
        /// Null events are ignored.
        /// </summary>
        public void Send(MidiEvent midiEvent)
        {
            if (midiEvent != null) queue.Enqueue(midiEvent);
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
                sampler.ProcessMidiEvent(midiEvent);
            return sampler.Read(buffer);
        }
    }
}
