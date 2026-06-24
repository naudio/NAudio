using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace NAudio.Sequencing
{
    /// <summary>
    /// The audio bridge that ties an <see cref="EventTimeline{T}"/> to a <see cref="MixingSampleProvider"/>.
    /// On each <see cref="Read"/> call it delegates to <see cref="EventBufferQuery"/> for the upcoming
    /// buffer's events, invokes the dispatcher delegate with sample-accurate frame offsets, then
    /// reads from the mixer to fill the output.
    /// </summary>
    /// <typeparam name="T">Event payload type.</typeparam>
    /// <remarks>
    /// Consumers that don't want to route events through a mixer (e.g. a VST3 instrument provider)
    /// should call <see cref="EventBufferQuery.Query{T}"/> directly instead of using this class.
    /// </remarks>
    public sealed class SequencedSampleProvider<T> : ISampleProvider
    {
        private readonly Transport transport;
        private readonly EventTimeline<T> timeline;
        private readonly MixingSampleProvider mixer;
        private readonly Action<SequencerEvent<T>, int> dispatcher;
        private IPositionTransform transform = IdentityPositionTransform.Instance;

        /// <summary>Creates a new sequenced sample provider.</summary>
        /// <param name="transport">Drives playback position. Its sample rate must match <paramref name="audioFormat"/>.</param>
        /// <param name="timeline">The source of events.</param>
        /// <param name="audioFormat">Mixer output format. Must be IEEE float.</param>
        /// <param name="dispatcher">Invoked for each event whose effective fire-frame lands inside the upcoming buffer.
        /// The integer is the frame offset within the buffer (0 = first frame). The dispatcher typically
        /// wraps the payload's audio in an <see cref="OffsetSampleProvider"/> set to <c>DelayBySamples = frameOffset * channels</c>
        /// and adds it to <see cref="Mixer"/>. The <see cref="SequencerEvent{T}.Tick"/> on the dispatched event is the
        /// effective tick (after the transform, and after loop iteration translation when looping).</param>
        public SequencedSampleProvider(Transport transport, EventTimeline<T> timeline,
            WaveFormat audioFormat, Action<SequencerEvent<T>, int> dispatcher)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            if (audioFormat is null) throw new ArgumentNullException(nameof(audioFormat));
            if (audioFormat.SampleRate != transport.SampleRate)
                throw new ArgumentException("Audio format sample rate must match the transport's sample rate.", nameof(audioFormat));
            mixer = new MixingSampleProvider(audioFormat) { ReadFully = true };
        }

        /// <summary>The transport this provider is driven by.</summary>
        public Transport Transport => transport;

        /// <summary>The event source. The caller adds/removes events on the timeline directly.</summary>
        public EventTimeline<T> Timeline => timeline;

        /// <summary>The internal mixer. Dispatchers add their voices here; external consumers can also
        /// add overlay providers (a metronome click, etc).</summary>
        public MixingSampleProvider Mixer => mixer;

        /// <summary>The active position transform. Defaults to identity (no swing).</summary>
        public IPositionTransform Transform
        {
            get => transform;
            set => transform = value ?? IdentityPositionTransform.Instance;
        }

        /// <inheritdoc/>
        public WaveFormat WaveFormat => mixer.WaveFormat;

        /// <inheritdoc/>
        public int Read(Span<float> buffer)
        {
            if (transport.IsPlaying)
            {
                int channels = WaveFormat.Channels;
                int frames = buffer.Length / channels;
                if (frames > 0)
                {
                    long startFrame = transport.CurrentFrames;
                    EventBufferQuery.Query(timeline, transport.TempoMap, transform, transport.Loop,
                                           startFrame, startFrame + frames, transport.SampleRate, dispatcher);
                    transport.AdvanceByFrames(frames);
                }
            }
            return mixer.Read(buffer);
        }
    }
}
