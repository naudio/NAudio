using System;

namespace NAudio.Dsp
{
    /// <summary>
    /// A single-channel circular delay line with integer and fractional (linearly
    /// interpolated) taps. The building block for delay, chorus, flanger and reverb
    /// effects. Writes are denormal-flushed so a decaying feedback tail cannot stall
    /// the CPU on subnormal arithmetic.
    /// </summary>
    public sealed class DelayLine
    {
        private readonly float[] buffer;
        private int writeIndex;

        /// <summary>
        /// Creates a delay line able to produce up to
        /// <paramref name="maxDelaySamples"/> samples of delay.
        /// </summary>
        /// <param name="maxDelaySamples">Maximum delay in samples. Must be at least 1.</param>
        public DelayLine(int maxDelaySamples)
        {
            if (maxDelaySamples < 1)
                throw new ArgumentOutOfRangeException(nameof(maxDelaySamples), "Maximum delay must be at least 1 sample");
            buffer = new float[maxDelaySamples];
        }

        /// <summary>
        /// Maximum delay in samples this line can produce.
        /// </summary>
        public int MaxDelaySamples => buffer.Length;

        /// <summary>
        /// Writes one sample into the line, advancing the write head.
        /// </summary>
        public void Write(float sample)
        {
            buffer[writeIndex] = DenormalGuard.Flush(sample);
            if (++writeIndex >= buffer.Length)
                writeIndex = 0;
        }

        /// <summary>
        /// Reads the sample <paramref name="delaySamples"/> samples in the past
        /// (1 = most recently written).
        /// </summary>
        /// <param name="delaySamples">Delay in samples, from 1 to <see cref="MaxDelaySamples"/>.</param>
        public float Read(int delaySamples)
        {
            if (delaySamples < 1 || delaySamples > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(delaySamples));
            var index = writeIndex - delaySamples;
            if (index < 0)
                index += buffer.Length;
            return buffer[index];
        }

        /// <summary>
        /// Reads a fractional delay using linear interpolation between the two
        /// neighbouring samples.
        /// </summary>
        /// <param name="delaySamples">Delay in samples (may be fractional), from 1 to <see cref="MaxDelaySamples"/>.</param>
        public float Read(float delaySamples)
        {
            if (delaySamples < 1f || delaySamples > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(delaySamples));
            var whole = (int)delaySamples;
            var fraction = delaySamples - whole;

            var i0 = writeIndex - whole;
            if (i0 < 0)
                i0 += buffer.Length;
            var i1 = i0 - 1;
            if (i1 < 0)
                i1 += buffer.Length;

            return buffer[i0] + fraction * (buffer[i1] - buffer[i0]);
        }

        /// <summary>
        /// Clears the delay line.
        /// </summary>
        public void Reset()
        {
            Array.Clear(buffer);
            writeIndex = 0;
        }
    }
}
