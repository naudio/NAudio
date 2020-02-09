using System;
using NAudio.Utils;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Converts a sample provider to 16 bit PCM, optionally clipping and adjusting volume along the way
    /// </summary>
    public class SampleToWaveProvider16 : IWaveProvider
    {
        private readonly ISampleProvider sourceProvider;
        private readonly WaveFormat waveFormat;
        private volatile float volume;
        private float[] sourceBuffer;

        /// <summary>
        /// Converts from an ISampleProvider (IEEE float) to a 16 bit PCM IWaveProvider.
        /// Number of channels and sample rate remain unchanged.
        /// </summary>
        /// <param name="sourceProvider">The input source provider</param>
        public SampleToWaveProvider16(ISampleProvider sourceProvider)
        {
            if (sourceProvider.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                throw new ArgumentException("Input source provider must be IEEE float", nameof(sourceProvider));
            if (sourceProvider.WaveFormat.BitsPerSample != 32)
                throw new ArgumentException("Input source provider must be 32 bit", nameof(sourceProvider));

            waveFormat = new WaveFormat(sourceProvider.WaveFormat.SampleRate, 16, sourceProvider.WaveFormat.Channels);

            this.sourceProvider = sourceProvider;
            volume = 1.0f;
        }

        /// <summary>
        /// Reads bytes from this wave stream
        /// </summary>
        /// <param name="destBuffer">The destination buffer</param>
        /// <param name="offset">Offset into the destination buffer</param>
        /// <param name="numBytes">Number of bytes read</param>
        /// <returns>Number of bytes read.</returns>
        public int Read(byte[] destBuffer, int offset, int numBytes)
        {
            int samplesRequired = numBytes / 2;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, samplesRequired);
            int sourceSamples = sourceProvider.Read(sourceBuffer, 0, samplesRequired);
            var destWaveBuffer = new WaveBuffer(destBuffer);

            int destOffset = offset / 2;
            for (int sample = 0; sample < sourceSamples; sample++)
            {
                // adjust volume
                float sample32 = sourceBuffer[sample] * volume;
                // clip
                if (sample32 > 1.0f)
                    sample32 = 1.0f;
                if (sample32 < -1.0f)
                    sample32 = -1.0f;
                destWaveBuffer.ShortBuffer[destOffset++] = (short)(sample32 * 32767);
            }

            return sourceSamples * 2;
        }

        /// <summary>
        /// <see cref="IWaveProvider.WaveFormat"/>
        /// </summary>
        public WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// Volume of this channel. 1.0 = full scale
        /// </summary>
        public float Volume
        {
            get { return volume; }
            set { volume = value; }
        }
    }
}
