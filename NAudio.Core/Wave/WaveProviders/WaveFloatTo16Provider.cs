using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Wave;
using NAudio.Utils;

namespace NAudio.Wave
{
    /// <summary>
    /// Converts IEEE float to 16 bit PCM, optionally clipping and adjusting volume along the way
    /// </summary>
    public class WaveFloatTo16Provider : IWaveProvider
    {
        private readonly IWaveProvider sourceProvider;
        private readonly WaveFormat waveFormat;
        private volatile float volume;
        private byte[] sourceBuffer;

        /// <summary>
        /// Creates a new WaveFloatTo16Provider
        /// </summary>
        /// <param name="sourceProvider">the source provider</param>
        public WaveFloatTo16Provider(IWaveProvider sourceProvider)
        {
            if (sourceProvider.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                throw new ArgumentException("Input wave provider must be IEEE float", "sourceProvider");
            if (sourceProvider.WaveFormat.BitsPerSample != 32)
                throw new ArgumentException("Input wave provider must be 32 bit", "sourceProvider");

            waveFormat = new WaveFormat(sourceProvider.WaveFormat.SampleRate, 16, sourceProvider.WaveFormat.Channels);

            this.sourceProvider = sourceProvider;
            this.volume = 1.0f;
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
            int sourceBytesRequired = numBytes * 2;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceBytesRequired);
            int sourceBytesRead = sourceProvider.Read(sourceBuffer, 0, sourceBytesRequired);
            WaveBuffer sourceWaveBuffer = new WaveBuffer(sourceBuffer);
            WaveBuffer destWaveBuffer = new WaveBuffer(destBuffer);

            int sourceSamples = sourceBytesRead / 4;
            int destOffset = offset / 2;
            for (int sample = 0; sample < sourceSamples; sample++)
            {
                // adjust volume
                float sample32 = sourceWaveBuffer.FloatBuffer[sample] * volume;
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
        public WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }

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
