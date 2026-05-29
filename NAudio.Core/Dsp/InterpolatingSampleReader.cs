using System;

namespace NAudio.Dsp
{
    /// <summary>
    /// Loop behaviour for a <see cref="SampleSource"/>.
    /// </summary>
    public enum LoopMode
    {
        /// <summary>Play once from start to end, no looping.</summary>
        None,
        /// <summary>Loop the region between the loop points continuously.</summary>
        Continuous,
        /// <summary>
        /// Loop the region between the loop points while the note is held, then
        /// play through to the end of the sample once the note is released.
        /// </summary>
        UntilRelease
    }

    /// <summary>
    /// An immutable block of mono sample data plus the playback metadata a
    /// sampler voice needs: the playable extent, the loop points and the loop
    /// mode. The data is interpreted by an <see cref="InterpolatingSampleReader"/>.
    /// Pitch/root-key handling lives in the voice, not here — this type only
    /// describes the raw sample and how it loops.
    /// </summary>
    public sealed class SampleSource
    {
        /// <summary>The mono sample data.</summary>
        public float[] Data { get; }

        /// <summary>The native sample rate of <see cref="Data"/> in Hz.</summary>
        public int SampleRate { get; }

        /// <summary>First playable sample index (inclusive).</summary>
        public int Start { get; }

        /// <summary>End playable sample index (exclusive).</summary>
        public int End { get; }

        /// <summary>Loop start sample index (inclusive).</summary>
        public int LoopStart { get; }

        /// <summary>Loop end sample index (exclusive) — the sample at this index is not played within the loop.</summary>
        public int LoopEnd { get; }

        /// <summary>How the sample loops.</summary>
        public LoopMode LoopMode { get; }

        /// <summary>
        /// Creates a sample source.
        /// </summary>
        /// <param name="data">Mono sample data.</param>
        /// <param name="sampleRate">Native sample rate in Hz.</param>
        /// <param name="loopMode">Loop behaviour.</param>
        /// <param name="start">First playable sample index, or null for 0.</param>
        /// <param name="end">End playable sample index (exclusive), or null for the full length.</param>
        /// <param name="loopStart">Loop start sample index, or null for <paramref name="start"/>.</param>
        /// <param name="loopEnd">Loop end sample index (exclusive), or null for <paramref name="end"/>.</param>
        public SampleSource(float[] data, int sampleRate, LoopMode loopMode = LoopMode.None,
            int? start = null, int? end = null, int? loopStart = null, int? loopEnd = null)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
            SampleRate = sampleRate;
            Start = start ?? 0;
            End = end ?? data.Length;
            LoopMode = loopMode;
            LoopStart = loopStart ?? Start;
            LoopEnd = loopEnd ?? End;

            if (Start < 0 || End > data.Length || Start > End)
                throw new ArgumentException("Invalid start/end range for sample data");
            if (loopMode != LoopMode.None)
            {
                if (LoopStart < Start || LoopEnd > End || LoopStart >= LoopEnd)
                    throw new ArgumentException("Invalid loop points for sample data");
            }
        }
    }

    /// <summary>
    /// Interpolation quality for an <see cref="InterpolatingSampleReader"/>.
    /// </summary>
    public enum InterpolationQuality
    {
        /// <summary>Nearest-neighbour (no interpolation) — fastest, lowest quality.</summary>
        None,
        /// <summary>Two-point linear interpolation.</summary>
        Linear,
        /// <summary>Four-point cubic Hermite interpolation — highest quality.</summary>
        Hermite
    }

    /// <summary>
    /// Reads a <see cref="SampleSource"/> at an arbitrary, per-sample-varying
    /// playback rate with interpolation, honouring the source's loop mode. This
    /// is the core pitch-shifting primitive of the sampler/synth: each voice owns
    /// one reader and supplies a playback increment (in source samples per output
    /// sample) that may change every sample under vibrato, pitch-bend or a pitch
    /// envelope. Unlike <see cref="WdlResampler"/> (a fixed-ratio block
    /// resampler), the increment is free to move continuously.
    /// Allocation-free in steady state.
    /// </summary>
    public sealed class InterpolatingSampleReader
    {
        private readonly SampleSource source;
        private double position;
        private bool released;
        private bool ended;

        /// <summary>
        /// Creates a reader positioned at the start of the source.
        /// </summary>
        public InterpolatingSampleReader(SampleSource source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            position = source.Start;
        }

        /// <summary>Interpolation quality. Defaults to <see cref="InterpolationQuality.Hermite"/>.</summary>
        public InterpolationQuality Quality { get; set; } = InterpolationQuality.Hermite;

        /// <summary>The current fractional read position, in source sample indices.</summary>
        public double Position => position;

        /// <summary>True once the reader has run past the end of the playable data.</summary>
        public bool Ended => ended;

        /// <summary>
        /// Signals note-off. For <see cref="LoopMode.UntilRelease"/> the reader
        /// stops looping and plays through to the end; for other modes it has no
        /// effect on looping.
        /// </summary>
        public void Release() => released = true;

        /// <summary>
        /// Whether the loop is currently active given the loop mode and release state.
        /// </summary>
        private bool LoopActive =>
            source.LoopMode == LoopMode.Continuous ||
            (source.LoopMode == LoopMode.UntilRelease && !released);

        /// <summary>
        /// Reads one output sample at the given playback increment and advances
        /// the read position. Returns 0 once the reader has ended.
        /// </summary>
        /// <param name="increment">Source samples to advance per output sample (1.0 = native pitch).</param>
        public float Read(double increment)
        {
            if (ended) return 0f;

            float sample = Interpolate(position);
            position += increment;
            WrapOrEnd();
            return sample;
        }

        /// <summary>
        /// Renders a block of output samples at a constant increment, returning
        /// the number of samples written before the reader ended (the remainder
        /// of the buffer is left untouched).
        /// </summary>
        /// <param name="buffer">Destination buffer.</param>
        /// <param name="increment">Source samples to advance per output sample.</param>
        public int Read(Span<float> buffer, double increment)
        {
            int written = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (ended) break;
                buffer[i] = Interpolate(position);
                position += increment;
                WrapOrEnd();
                written++;
            }
            return written;
        }

        private void WrapOrEnd()
        {
            if (LoopActive)
            {
                double loopLength = source.LoopEnd - source.LoopStart;
                // a large increment could overshoot multiple loop lengths
                while (position >= source.LoopEnd)
                {
                    position -= loopLength;
                }
            }
            else if (position >= source.End)
            {
                ended = true;
            }
        }

        private float Interpolate(double pos)
        {
            int i = (int)Math.Floor(pos);
            float frac = (float)(pos - i);

            switch (Quality)
            {
                case InterpolationQuality.None:
                    return SampleAt(i);
                case InterpolationQuality.Linear:
                {
                    float a = SampleAt(i);
                    float b = SampleAt(i + 1);
                    return a + (b - a) * frac;
                }
                default: // Hermite (4-point, 3rd-order)
                {
                    float xm1 = SampleAt(i - 1);
                    float x0 = SampleAt(i);
                    float x1 = SampleAt(i + 1);
                    float x2 = SampleAt(i + 2);

                    float c = (x1 - xm1) * 0.5f;
                    float v = x0 - x1;
                    float w = c + v;
                    float a = w + v + (x2 - x0) * 0.5f;
                    float bNeg = w + a;
                    return ((a * frac - bNeg) * frac + c) * frac + x0;
                }
            }
        }

        /// <summary>
        /// Resolves a (possibly out-of-range) sample index to a value, wrapping
        /// into the loop region when the loop is active so that interpolation
        /// across the loop boundary is continuous, and clamping to the playable
        /// extent otherwise.
        /// </summary>
        private float SampleAt(int index)
        {
            if (LoopActive)
            {
                int loopLength = source.LoopEnd - source.LoopStart;
                if (index >= source.LoopEnd)
                {
                    index = source.LoopStart + (index - source.LoopStart) % loopLength;
                }
                else if (index < source.LoopStart)
                {
                    int offset = (source.LoopStart - index) % loopLength;
                    index = offset == 0 ? source.LoopStart : source.LoopEnd - offset;
                }
            }
            else
            {
                if (index < source.Start) index = source.Start;
                else if (index >= source.End) index = source.End - 1;
            }
            return source.Data[index];
        }
    }
}
