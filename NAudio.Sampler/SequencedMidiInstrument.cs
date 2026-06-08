using System;
using System.Collections.Generic;
using NAudio.Midi;
using NAudio.Sequencing;
using NAudio.Wave;

namespace NAudio.Sampler
{
    /// <summary>
    /// Plays an <see cref="EventTimeline{T}"/> of MIDI events on a
    /// <see cref="SamplerEngine"/>, driven by a <see cref="Transport"/>. Unlike
    /// <see cref="SequencedSampleProvider{T}"/> (which spawns a sample provider
    /// per event into a mixer), this drives a single stateful polyphonic
    /// instrument: on each <see cref="Read"/> it dispatches the buffer's MIDI
    /// events to the sampler at their exact frame offset, rendering the sampler in
    /// segments between events so timing is sample-accurate within the block.
    /// </summary>
    public sealed class SequencedMidiInstrument : ISampleProvider
    {
        private readonly Transport transport;
        private readonly EventTimeline<MidiEvent> timeline;
        private readonly SamplerEngine sampler;
        private readonly List<(int Offset, MidiEvent Event)> pending = new();
        private readonly Action<SequencerEvent<MidiEvent>, int> collect;

        /// <summary>Creates the bridge. The sampler's sample rate must match the transport's.</summary>
        public SequencedMidiInstrument(Transport transport, EventTimeline<MidiEvent> timeline, SamplerEngine sampler)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            this.sampler = sampler ?? throw new ArgumentNullException(nameof(sampler));
            if (sampler.WaveFormat.SampleRate != transport.SampleRate)
                throw new ArgumentException("Sampler sample rate must match the transport's sample rate.", nameof(sampler));

            collect = (ev, frameOffset) => pending.Add((frameOffset, ev.Payload));
        }

        /// <summary>The transport driving playback position.</summary>
        public Transport Transport => transport;

        /// <inheritdoc/>
        public WaveFormat WaveFormat => sampler.WaveFormat;

        /// <inheritdoc/>
        public int Read(Span<float> buffer)
        {
            int channels = WaveFormat.Channels;
            int frames = buffer.Length / channels;
            if (frames == 0) return 0;

            // when stopped, still pull the sampler so envelope tails ring out
            if (!transport.IsPlaying)
                return sampler.Read(buffer);

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
                    sampler.Read(buffer.Slice(framePos * channels, (at - framePos) * channels));
                    framePos = at;
                }
                sampler.ProcessMidiEvent(midiEvent);
            }

            if (framePos < frames)
                sampler.Read(buffer.Slice(framePos * channels, (frames - framePos) * channels));

            transport.AdvanceByFrames(frames);
            return buffer.Length;
        }
    }
}
