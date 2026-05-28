using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace NAudio.Sequencing
{
    /// <summary>
    /// The audio bridge that ties an <see cref="EventTimeline{T}"/> to a <see cref="MixingSampleProvider"/>.
    /// On each <see cref="Read"/> call it queries the timeline for events whose effective fire-frame
    /// lands inside the upcoming buffer, applies the active <see cref="IPositionTransform"/> (e.g. swing),
    /// and invokes the dispatcher delegate with a sub-buffer frame offset for each event. The dispatcher
    /// is responsible for translating the payload into mixer inputs (or any other side effect).
    /// </summary>
    /// <typeparam name="T">Event payload type.</typeparam>
    public sealed class SequencedSampleProvider<T> : ISampleProvider
    {
        // Cap iteration walks for pathological inputs (e.g. someone seeking with a zero-length loop).
        private const long MaxLoopIterationsPerRead = 1_000_000;

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
        /// wraps the payload's audio in an <see cref="OffsetSampleProvider"/> set to <c>DelayBy = frameOffset * channels</c>
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
                    DispatchEventsForBuffer(frames);
                    transport.AdvanceByFrames(frames);
                }
            }
            return mixer.Read(buffer);
        }

        // The filter that decides whether each candidate event fires is in FRAME space, not tick space —
        // truncation when converting seconds→ticks can otherwise miss events sitting exactly on a buffer
        // boundary. The tick range we query is intentionally over-scanned to account for both the position
        // transform's shift and 1-tick boundary precision.
        private void DispatchEventsForBuffer(int frames)
        {
            var tempoMap = transport.TempoMap;
            var sampleRate = transport.SampleRate;
            long startFrame = transport.CurrentFrames;
            long endFrameExcl = startFrame + frames;
            var secStart = startFrame / (double)sampleRate;
            var secEnd = endFrameExcl / (double)sampleRate;
            long absStartTick = tempoMap.TicksFromSeconds(secStart);
            long absEndTick = tempoMap.TicksFromSeconds(secEnd);
            long maxShift = transform.MaxShiftTicks;

            if (transport.Loop is LoopRegion loop)
            {
                DispatchLooped(loop, absStartTick, absEndTick, startFrame, endFrameExcl, sampleRate, maxShift, tempoMap);
            }
            else
            {
                DispatchAbsolute(absStartTick, absEndTick, startFrame, endFrameExcl, sampleRate, maxShift, tempoMap);
            }
        }

        private void DispatchAbsolute(long absStartTick, long absEndTick, long startFrame, long endFrameExcl,
            int sampleRate, long maxShift, ITempoMap tempoMap)
        {
            long queryStart = Math.Max(0, absStartTick - maxShift - 1);
            long queryEnd = absEndTick + maxShift + 2;
            var candidates = timeline.EventsInRange(queryStart, queryEnd);
            for (int i = 0; i < candidates.Count; i++)
            {
                var ev = candidates[i];
                long effective = transform.Transform(ev.Tick);
                long eventFrame = (long)(tempoMap.SecondsFromTicks(effective) * sampleRate);
                if (eventFrame < startFrame || eventFrame >= endFrameExcl) continue;
                int frameOffset = (int)(eventFrame - startFrame);
                dispatcher(new SequencerEvent<T>(effective, ev.Payload), frameOffset);
            }
        }

        private void DispatchLooped(LoopRegion loop, long absStartTick, long absEndTick, long startFrame, long endFrameExcl,
            int sampleRate, long maxShift, ITempoMap tempoMap)
        {
            // Pre-loop region: dispatch absolute up to loop.StartTick, then enter the loop walk.
            if (absStartTick < loop.StartTick)
            {
                DispatchAbsolute(absStartTick, Math.Min(absEndTick, loop.StartTick),
                                 startFrame, endFrameExcl, sampleRate, maxShift, tempoMap);
                if (absEndTick <= loop.StartTick) return;
                absStartTick = loop.StartTick;
            }

            long loopLen = loop.LengthTicks;
            long iteration = (absStartTick - loop.StartTick) / loopLen;
            long queryStart = Math.Max(0, loop.StartTick - maxShift - 1);
            long queryEnd = loop.EndTick + maxShift + 2;
            var candidates = timeline.EventsInRange(queryStart, queryEnd);

            // Walk iterations until we move past the buffer. The frame filter is the authoritative gate.
            for (long n = 0; n < MaxLoopIterationsPerRead; n++)
            {
                long iterAbsStart = loop.StartTick + iteration * loopLen;
                long iterStartFrame = (long)(tempoMap.SecondsFromTicks(iterAbsStart) * sampleRate);
                if (iterStartFrame >= endFrameExcl) break;

                for (int i = 0; i < candidates.Count; i++)
                {
                    var ev = candidates[i];
                    long effective = transform.Transform(ev.Tick);
                    long absoluteEffective = effective + iteration * loopLen;
                    long eventFrame = (long)(tempoMap.SecondsFromTicks(absoluteEffective) * sampleRate);
                    if (eventFrame < startFrame || eventFrame >= endFrameExcl) continue;
                    int frameOffset = (int)(eventFrame - startFrame);
                    dispatcher(new SequencerEvent<T>(absoluteEffective, ev.Payload), frameOffset);
                }

                iteration++;
            }
        }
    }
}
