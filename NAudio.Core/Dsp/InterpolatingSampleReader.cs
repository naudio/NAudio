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
        /// Length, in samples, of the crossfade applied at the loop seam (0 = none).
        /// As the read position approaches <see cref="LoopEnd"/> the outgoing audio
        /// is blended into the lead-in before <see cref="LoopStart"/>, so the loop
        /// wraps without the click an abrupt jump between mismatched samples causes.
        /// Clamped to the data available before the loop and to the loop length.
        /// </summary>
        public int CrossfadeSamples { get; }

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
        /// <param name="crossfadeSamples">Loop-seam crossfade length in samples (0 = none).</param>
        public SampleSource(float[] data, int sampleRate, LoopMode loopMode = LoopMode.None,
            int? start = null, int? end = null, int? loopStart = null, int? loopEnd = null,
            int crossfadeSamples = 0)
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

                // the incoming side reads from [LoopStart - xfade, LoopStart), so the
                // crossfade can be no longer than the lead-in before the loop, nor
                // than the loop itself
                int maxByLeadIn = LoopStart - Start;
                int maxByLoop = LoopEnd - LoopStart - 1;
                CrossfadeSamples = Math.Max(0, Math.Min(crossfadeSamples, Math.Min(maxByLeadIn, maxByLoop)));
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
        /// <summary>
        /// Four-point cubic Hermite interpolation — the recommended default,
        /// transparent for downward and moderate pitch shifts. A higher-quality
        /// windowed-sinc option may be added later for extreme upward shifts
        /// (see Docs/Architecture/SamplerDesign.md §11).
        /// </summary>
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
        private bool hasLooped; // true once the read position has wrapped at least once

        /// <summary>
        /// Creates a reader positioned at the start of the source.
        /// </summary>
        public InterpolatingSampleReader(SampleSource source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            position = source.Start;
        }

        /// <summary>
        /// Interpolation quality. Defaults to <see cref="InterpolationQuality.Hermite"/>,
        /// the recommended general-purpose setting.
        /// </summary>
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

            float sample = CurrentSample();
            position += increment;
            WrapOrEnd();
            return sample;
        }

        // the sample at the current position, with the loop-seam crossfade applied
        // when enabled and within the crossfade zone before LoopEnd
        private float CurrentSample()
        {
            int xfade = source.CrossfadeSamples;
            if (xfade > 0 && LoopActive)
            {
                double zoneStart = source.LoopEnd - xfade;
                if (position >= zoneStart)
                {
                    double t = (position - zoneStart) / xfade; // 0 at zone start, ->1 at LoopEnd
                    double loopLength = source.LoopEnd - source.LoopStart;
                    // outgoing: the tail of the loop (uses the normal loop-wrapping read).
                    // incoming: the lead-in just before LoopStart, read raw (no loop wrap,
                    // which would otherwise fold it back onto the outgoing tail).
                    float outgoing = Interpolate(position);
                    float incoming = Interpolate(position - loopLength, loopWrap: false);
                    return (float)(outgoing * (1.0 - t) + incoming * t);
                }
            }
            return Interpolate(position);
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
                buffer[i] = CurrentSample();
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
                    hasLooped = true;
                }
            }
            else if (position >= source.End)
            {
                ended = true;
            }
        }

        private float Interpolate(double pos, bool loopWrap = true)
        {
            int i = (int)Math.Floor(pos);
            float frac = (float)(pos - i);

            switch (Quality)
            {
                case InterpolationQuality.None:
                    return SampleAt(i, loopWrap);
                case InterpolationQuality.Linear:
                {
                    float a = SampleAt(i, loopWrap);
                    float b = SampleAt(i + 1, loopWrap);
                    return a + (b - a) * frac;
                }
                default: // Hermite (4-point, 3rd-order)
                {
                    float xm1 = SampleAt(i - 1, loopWrap);
                    float x0 = SampleAt(i, loopWrap);
                    float x1 = SampleAt(i + 1, loopWrap);
                    float x2 = SampleAt(i + 2, loopWrap);

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
        private float SampleAt(int index, bool loopWrap = true)
        {
            if (loopWrap && LoopActive)
            {
                int loopLength = source.LoopEnd - source.LoopStart;
                if (index >= source.LoopEnd)
                {
                    // lookahead past the loop end wraps to the loop start for seam continuity
                    index = source.LoopStart + (index - source.LoopStart) % loopLength;
                }
                else if (index < source.LoopStart && hasLooped)
                {
                    // wrap a look-back before the loop start to the loop end — but only
                    // once we're actually looping. On the first pass, indices in
                    // [Start, LoopStart) are the real lead-in/attack and must read raw.
                    int offset = (source.LoopStart - index) % loopLength;
                    index = offset == 0 ? source.LoopStart : source.LoopEnd - offset;
                }
            }

            // clamp to the playable extent (covers non-looping reads, the pre-loop
            // lead-in, and the raw crossfade incoming read)
            if (index < source.Start) index = source.Start;
            else if (index >= source.End) index = source.End - 1;
            return source.Data[index];
        }
    }
}
