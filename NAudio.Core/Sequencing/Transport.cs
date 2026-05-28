using System;
using System.Threading;

namespace NAudio.Sequencing
{
    /// <summary>
    /// Tracks playback position for the sequencing layer. Position is held in both audio frames
    /// (the master, since the audio device pulls a frame count) and canonical ticks (derived from
    /// the tempo map). Mutating methods (<see cref="Play"/>, <see cref="Stop"/>, <see cref="SeekTicks"/>,
    /// <see cref="SeekFrames"/>) are safe to call from any thread, but coarse-grained: a seek issued
    /// while a buffer is being rendered takes effect on the next buffer.
    /// </summary>
    public sealed class Transport
    {
        private readonly ITempoMap tempoMap;
        private readonly int sampleRate;
        private long currentFrames;
        private long currentTicks;
        private int isPlaying;

        /// <summary>Creates a new transport at position 0, not playing.</summary>
        public Transport(ITempoMap tempoMap, int sampleRate)
        {
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be positive.");
            this.tempoMap = tempoMap ?? throw new ArgumentNullException(nameof(tempoMap));
            this.sampleRate = sampleRate;
        }

        /// <summary>The tempo map driving real-time → tick conversion.</summary>
        public ITempoMap TempoMap => tempoMap;

        /// <summary>The sample rate, in frames per second.</summary>
        public int SampleRate => sampleRate;

        /// <summary>An optional loop region. When set, <see cref="CurrentTicks"/> is unbounded but consumers
        /// (e.g. <see cref="SequencedSampleProvider{T}"/>) wrap timeline queries through this region.</summary>
        public LoopRegion? Loop { get; set; }

        /// <summary>The current playhead position in audio frames since the transport was last seeked or constructed.</summary>
        public long CurrentFrames => Interlocked.Read(ref currentFrames);

        /// <summary>The current playhead position in canonical ticks.</summary>
        public long CurrentTicks => Interlocked.Read(ref currentTicks);

        /// <summary>Whether the transport is currently playing. Used by consumers to gate event dispatch.</summary>
        public bool IsPlaying => Volatile.Read(ref isPlaying) != 0;

        /// <summary>Begins playback. Subsequent calls to <see cref="AdvanceByFrames"/> will advance the playhead.</summary>
        public void Play() => Volatile.Write(ref isPlaying, 1);

        /// <summary>Stops playback. Position is preserved.</summary>
        public void Stop() => Volatile.Write(ref isPlaying, 0);

        /// <summary>Seeks the playhead to the given tick.</summary>
        public void SeekTicks(long ticks)
        {
            if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks), "Ticks must not be negative.");
            var frames = (long)(tempoMap.SecondsFromTicks(ticks) * sampleRate);
            Interlocked.Exchange(ref currentTicks, ticks);
            Interlocked.Exchange(ref currentFrames, frames);
        }

        /// <summary>Seeks the playhead to the given frame.</summary>
        public void SeekFrames(long frames)
        {
            if (frames < 0) throw new ArgumentOutOfRangeException(nameof(frames), "Frames must not be negative.");
            var ticks = tempoMap.TicksFromSeconds((double)frames / sampleRate);
            Interlocked.Exchange(ref currentFrames, frames);
            Interlocked.Exchange(ref currentTicks, ticks);
        }

        /// <summary>
        /// Advances the playhead by the given number of frames. Called by the consumer after rendering
        /// (and dispatching events for) a buffer. The tick position is recomputed from the cumulative
        /// frame count via the tempo map so it cannot drift.
        /// </summary>
        public void AdvanceByFrames(int frames)
        {
            if (frames < 0) throw new ArgumentOutOfRangeException(nameof(frames), "Frames must not be negative.");
            if (frames == 0) return;
            var newFrames = Interlocked.Read(ref currentFrames) + frames;
            var newTicks = tempoMap.TicksFromSeconds((double)newFrames / sampleRate);
            Interlocked.Exchange(ref currentFrames, newFrames);
            Interlocked.Exchange(ref currentTicks, newTicks);
        }
    }
}
