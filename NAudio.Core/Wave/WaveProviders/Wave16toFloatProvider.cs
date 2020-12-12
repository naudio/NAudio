using System;
using System.Runtime.InteropServices;
using NAudio.Utils;

namespace NAudio.Wave
{
    /// <summary>
    /// Converts 16 bit PCM to IEEE float, optionally adjusting volume along the way
    /// </summary>
    public class Wave16ToFloatProvider : IWaveProvider
    {
        private IWaveProvider sourceProvider;
        private readonly WaveFormat waveFormat;
        private volatile float volume;
        private byte[] sourceBuffer;

        /// <summary>
        /// Creates a new Wave16toFloatProvider
        /// </summary>
        /// <param name="sourceProvider">the source provider</param>
        public Wave16ToFloatProvider(IWaveProvider sourceProvider)
        {
            if (sourceProvider.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
                throw new ArgumentException("Only PCM supported");
            if (sourceProvider.WaveFormat.BitsPerSample != 16)
                throw new ArgumentException("Only 16 bit audio supported");

            waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sourceProvider.WaveFormat.SampleRate, sourceProvider.WaveFormat.Channels);

            this.sourceProvider = sourceProvider;
            this.volume = 1.0f;
        }

        /// <summary>
        /// Reads bytes from this wave stream
        /// </summary>
        /// <param name="destBuffer">The destination buffer</param>
        /// <returns>Number of bytes read.</returns>
        public int Read(Span<byte> destBuffer)
        {
            int sourceBytesRequired = destBuffer.Length / 2;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceBytesRequired);
            var sourceSpan = new Span<byte>(sourceBuffer,0, sourceBytesRequired);
            int sourceBytesRead = sourceProvider.Read(sourceSpan);
            var sourceWaveBuffer = MemoryMarshal.Cast<byte, short>(sourceSpan);
            var destWaveBuffer = MemoryMarshal.Cast<byte, float>(destBuffer);

            int sourceSamples = sourceBytesRead / 2;
            int destOffset = 0;
            for (int sample = 0; sample < sourceSamples; sample++)
            {
                destWaveBuffer[destOffset++] = (sourceWaveBuffer[sample] / 32768f) * volume;
            }

            return sourceSamples * 4;
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
