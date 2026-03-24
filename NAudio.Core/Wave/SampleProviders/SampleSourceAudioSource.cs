using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Adapts an <see cref="ISampleSource"/> to an <see cref="IAudioSource"/> by reinterpreting
    /// the float span as a byte span. Only works with IEEE float format.
    /// </summary>
    public class SampleSourceAudioSource : IAudioSource
    {
        private readonly ISampleSource source;

        /// <summary>
        /// Creates a new SampleSourceAudioSource wrapping an existing ISampleSource.
        /// </summary>
        public SampleSourceAudioSource(ISampleSource source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            if (source.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                throw new ArgumentException("ISampleSource must be IEEE float format", nameof(source));
            }
        }

        /// <inheritdoc/>
        public WaveFormat WaveFormat => source.WaveFormat;

        /// <inheritdoc/>
        public int Read(Span<byte> buffer)
        {
            var floatSpan = MemoryMarshal.Cast<byte, float>(buffer);
            int samplesRead = source.Read(floatSpan);
            return samplesRead * sizeof(float);
        }
    }
}
