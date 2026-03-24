using System;
using System.Buffers;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Adapts an <see cref="ISampleSource"/> to the <see cref="ISampleProvider"/> interface,
    /// allowing span-based sample sources to be used with legacy float[]-based consumers.
    /// </summary>
    public class SampleSourceSampleProvider : ISampleProvider
    {
        private readonly ISampleSource source;

        /// <summary>
        /// Creates a new SampleSourceSampleProvider wrapping an existing ISampleSource.
        /// </summary>
        public SampleSourceSampleProvider(ISampleSource source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <inheritdoc/>
        public WaveFormat WaveFormat => source.WaveFormat;

        /// <inheritdoc/>
        public int Read(float[] buffer, int offset, int count)
        {
            return source.Read(buffer.AsSpan(offset, count));
        }
    }
}
