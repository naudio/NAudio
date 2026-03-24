using System;
using NAudio.Utils;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Adapts an existing <see cref="ISampleProvider"/> to the <see cref="ISampleSource"/> interface
    /// using a bridge buffer between the float[] and Span APIs.
    /// </summary>
    public class SampleProviderSampleSource : ISampleSource
    {
        private readonly ISampleProvider provider;
        private float[] bridgeBuffer;

        /// <summary>
        /// Creates a new SampleProviderSampleSource wrapping an existing ISampleProvider.
        /// </summary>
        public SampleProviderSampleSource(ISampleProvider provider)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <inheritdoc/>
        public WaveFormat WaveFormat => provider.WaveFormat;

        /// <inheritdoc/>
        public int Read(Span<float> buffer)
        {
            bridgeBuffer = BufferHelpers.Ensure(bridgeBuffer, buffer.Length);
            int samplesRead = provider.Read(bridgeBuffer, 0, buffer.Length);
            if (samplesRead > 0)
            {
                bridgeBuffer.AsSpan(0, samplesRead).CopyTo(buffer);
            }
            return samplesRead;
        }
    }
}
