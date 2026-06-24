using System;
using System.Collections.Generic;
using NAudio.Sequencing;
using NAudio.Wave;

namespace NAudio.Midi
{
    /// <summary>
    /// Plays an <see cref="EventTimeline{T}"/> of MIDI events on an
    /// <see cref="IMidiInstrument"/>, driven by a <see cref="Transport"/>. Unlike
    /// <see cref="SequencedSampleProvider{T}"/> (which spawns a sample provider
    /// per event into a mixer), this drives a single stateful polyphonic
    /// instrument: on each <see cref="Read"/> it dispatches the buffer's MIDI
    /// events to the instrument at their exact frame offset, rendering the
    /// instrument in segments between events so timing is sample-accurate within
    /// the block.
    /// </summary>
    /// <remarks>
    /// The steady-state <see cref="Read"/> path performs no heap allocations and
    /// takes no lock on the timeline: event queries run over
    /// <see cref="EventTimeline{T}"/>'s lock-free immutable snapshot, and the
    /// per-buffer event list and dispatch delegate are allocated once up front.
    /// </remarks>
    public sealed class SequencedMidiPlayer : ISampleProvider
    {
        private readonly Transport transport;
        private readonly EventTimeline<MidiEvent> timeline;
        private readonly IMidiInstrument instrument;
        private readonly List<(int Offset, MidiEvent Event)> pending = new();
        private readonly Action<SequencerEvent<MidiEvent>, int> collect;

        /// <summary>Creates the bridge. The instrument's sample rate must match the transport's.</summary>
        public SequencedMidiPlayer(Transport transport, EventTimeline<MidiEvent> timeline, IMidiInstrument instrument)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            this.instrument = instrument ?? throw new ArgumentNullException(nameof(instrument));
            if (instrument.WaveFormat.SampleRate != transport.SampleRate)
                throw new ArgumentException("Instrument sample rate must match the transport's sample rate.", nameof(instrument));

            collect = (ev, frameOffset) => pending.Add((frameOffset, ev.Payload));
        }

        /// <summary>The transport driving playback position.</summary>
        public Transport Transport => transport;

        /// <summary>The instrument the timeline plays.</summary>
        public IMidiInstrument Instrument => instrument;

        /// <inheritdoc/>
        public WaveFormat WaveFormat => instrument.WaveFormat;

        /// <inheritdoc/>
        public int Read(Span<float> buffer)
        {
            int channels = WaveFormat.Channels;
            int frames = buffer.Length / channels;
            if (frames == 0) return 0;

            // when stopped, still pull the instrument so envelope tails ring out
            if (!transport.IsPlaying)
                return instrument.Read(buffer);

            pending.Clear();
            long startFrame = transport.CurrentFrames;
            EventBufferQuery.Query(timeline, transport.TempoMap, IdentityPositionTransform.Instance,
                                   transport.Loop, startFrame, startFrame + frames, transport.SampleRate, collect);

            int framePos = 0;
            foreach (var (offset, midiEvent) in pending)
            {
                int at = offset < 0 ? 0 : offset > frames ? frames : offset;
                if (at > framePos)
                {
                    instrument.Read(buffer.Slice(framePos * channels, (at - framePos) * channels));
                    framePos = at;
                }
                instrument.ProcessMidiEvent(midiEvent);
            }

            if (framePos < frames)
                instrument.Read(buffer.Slice(framePos * channels, (frames - framePos) * channels));

            transport.AdvanceByFrames(frames);
            return buffer.Length;
        }
    }
}
