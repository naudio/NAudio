using System;

namespace NAudio.Sequencing
{
    /// <summary>
    /// Stateless per-buffer event query — the dispatch math used by
    /// <see cref="SequencedSampleProvider{T}"/>, exposed as a standalone API for consumers that
    /// don't want to route events through a <see cref="NAudio.Wave.SampleProviders.MixingSampleProvider"/>
    /// (e.g. a VST3 instrument provider feeding events into a plugin's <c>process()</c> event input).
    /// </summary>
    /// <remarks>
    /// Stateless by design: the caller supplies an arbitrary <c>[startFrame, endFrameExclusive)</c>
    /// range, so a host that pulls e.g. 4096 frames from WASAPI can split the work across multiple
    /// downstream <c>process()</c> calls if the plugin's max block size is smaller. The dispatcher
    /// receives each event with its effective absolute tick (after the position transform and, in a
    /// loop, after iteration translation) and a sample-accurate frame offset relative to <c>startFrame</c>.
    /// </remarks>
    public static class EventBufferQuery
    {
        // Cap iteration walks for pathological inputs (e.g. caller queries a huge range with a
        // tiny loop). Same value as the original SequencedSampleProvider safeguard.
        private const long MaxLoopIterationsPerCall = 1_000_000;

        /// <summary>
        /// Invokes <paramref name="dispatcher"/> once per event whose effective fire-frame lands
        /// inside <c>[startFrame, endFrameExclusive)</c>.
        /// </summary>
        public static void Query<T>(
            EventTimeline<T> timeline,
            ITempoMap tempoMap,
            IPositionTransform transform,
            LoopRegion? loop,
            long startFrame,
            long endFrameExclusive,
            int sampleRate,
            Action<SequencerEvent<T>, int> dispatcher)
        {
            if (timeline is null) throw new ArgumentNullException(nameof(timeline));
            if (tempoMap is null) throw new ArgumentNullException(nameof(tempoMap));
            if (transform is null) throw new ArgumentNullException(nameof(transform));
            if (dispatcher is null) throw new ArgumentNullException(nameof(dispatcher));
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be positive.");
            if (startFrame < 0) throw new ArgumentOutOfRangeException(nameof(startFrame), "Start frame must not be negative.");
            if (endFrameExclusive <= startFrame) return;

            var secStart = startFrame / (double)sampleRate;
            var secEnd = endFrameExclusive / (double)sampleRate;
            long absStartTick = tempoMap.TicksFromSeconds(secStart);
            long absEndTick = tempoMap.TicksFromSeconds(secEnd);
            long maxShift = transform.MaxShiftTicks;

            if (loop is LoopRegion l)
            {
                QueryLooped(timeline, tempoMap, transform, l, absStartTick, absEndTick,
                            startFrame, endFrameExclusive, sampleRate, maxShift, dispatcher);
            }
            else
            {
                QueryAbsolute(timeline, tempoMap, transform, absStartTick, absEndTick,
                              startFrame, endFrameExclusive, sampleRate, maxShift, dispatcher);
            }
        }

        private static void QueryAbsolute<T>(
            EventTimeline<T> timeline, ITempoMap tempoMap, IPositionTransform transform,
            long absStartTick, long absEndTick, long startFrame, long endFrameExcl,
            int sampleRate, long maxShift, Action<SequencerEvent<T>, int> dispatcher)
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

        private static void QueryLooped<T>(
            EventTimeline<T> timeline, ITempoMap tempoMap, IPositionTransform transform,
            LoopRegion loop, long absStartTick, long absEndTick, long startFrame, long endFrameExcl,
            int sampleRate, long maxShift, Action<SequencerEvent<T>, int> dispatcher)
        {
            if (absStartTick < loop.StartTick)
            {
                QueryAbsolute(timeline, tempoMap, transform,
                              absStartTick, Math.Min(absEndTick, loop.StartTick),
                              startFrame, endFrameExcl, sampleRate, maxShift, dispatcher);
                if (absEndTick <= loop.StartTick) return;
                absStartTick = loop.StartTick;
            }

            long loopLen = loop.LengthTicks;
            long iteration = (absStartTick - loop.StartTick) / loopLen;
            // Query strictly within the loop's half-open tick range. Events at exactly loop.EndTick
            // belong to the next iteration's StartTick, not the current iteration — no over-scan on
            // either side: the frame filter below is the authoritative bound, and swing-forward shifts
            // of events nominally inside the loop can still land past loop.EndTick and fire correctly.
            var candidates = timeline.EventsInRange(loop.StartTick, loop.EndTick);

            for (long n = 0; n < MaxLoopIterationsPerCall; n++)
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
