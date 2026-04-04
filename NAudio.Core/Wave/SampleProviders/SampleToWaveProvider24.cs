using System;
using NAudio.Utils;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Converts a sample source to 24 bit PCM, optionally clipping and adjusting volume along the way
    /// </summary>
    public class SampleToWaveProvider24 : IWaveProvider
    {
        private readonly ISampleProvider sourceProvider;
        private readonly WaveFormat waveFormat;
        private volatile float volume;
        private float[] sourceBuffer;

        /// <summary>
        /// Converts from an ISampleProvider (IEEE float) to a 24 bit PCM IWaveProvider.
        /// Number of channels and sample rate remain unchanged.
        /// </summary>
        /// <param name="sourceProvider">The input source provider</param>
        public SampleToWaveProvider24(ISampleProvider sourceProvider)
        {
            if (sourceProvider.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                throw new ArgumentException("Input source provider must be IEEE float", nameof(sourceProvider));
            if (sourceProvider.WaveFormat.BitsPerSample != 32)
                throw new ArgumentException("Input source provider must be 32 bit", nameof(sourceProvider));

            waveFormat = new WaveFormat(sourceProvider.WaveFormat.SampleRate, 24, sourceProvider.WaveFormat.Channels);

            this.sourceProvider = sourceProvider;
            volume = 1.0f;
        }

        /// <summary>
        /// Reads bytes from this wave stream, clipping if necessary
        /// </summary>
        /// <param name="buffer">The destination buffer</param>
        /// <returns>Number of bytes read.</returns>
        public int Read(Span<byte> buffer)
        {
            var samplesRequired = buffer.Length / 3;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, samplesRequired);
            var sourceSamples = sourceProvider.Read(sourceBuffer.AsSpan(0, samplesRequired));

            int destOffset = 0;
            for (var sample = 0; sample < sourceSamples; sample++)
            {
                // adjust volume
                var sample32 = sourceBuffer[sample] * volume;
                // clip
                if (sample32 > 1.0f)
                    sample32 = 1.0f;
                if (sample32 < -1.0f)
                    sample32 = -1.0f;

                var sample24 = (int) (sample32*8388607.0);
                buffer[destOffset++] = (byte)(sample24);
                buffer[destOffset++] = (byte)(sample24 >> 8);
                buffer[destOffset++] = (byte)(sample24 >> 16);
            }

            return sourceSamples * 3;
        }

        /// <summary>
        /// The Format of this IWaveProvider
        /// <see cref="IWaveProvider.WaveFormat"/>
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }

        /// <summary>
        /// Volume of this channel. 1.0 = full scale, 0.0 to mute
        /// </summary>
        public float Volume
        {
            get { return volume; }
            set { volume = value; }
        }
    }
}
