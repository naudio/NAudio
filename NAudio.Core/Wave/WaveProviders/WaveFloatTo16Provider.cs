using System;
using System.Runtime.InteropServices;
using NAudio.Utils;

namespace NAudio.Wave
{
    /// <summary>
    /// Converts IEEE float to 16 bit PCM, optionally clipping and adjusting volume along the way
    /// </summary>
    public class WaveFloatTo16Provider : IAudioSource
    {
        private readonly IAudioSource sourceProvider;
        private readonly WaveFormat waveFormat;
        private volatile float volume;
        private byte[] sourceBuffer;

        /// <summary>
        /// Creates a new WaveFloatTo16Provider
        /// </summary>
        /// <param name="sourceProvider">the source provider</param>
        public WaveFloatTo16Provider(IAudioSource sourceProvider)
        {
            if (sourceProvider.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                throw new ArgumentException("Input wave provider must be IEEE float", nameof(sourceProvider));
            if (sourceProvider.WaveFormat.BitsPerSample != 32)
                throw new ArgumentException("Input wave provider must be 32 bit", nameof(sourceProvider));

            waveFormat = new WaveFormat(sourceProvider.WaveFormat.SampleRate, 16, sourceProvider.WaveFormat.Channels);

            this.sourceProvider = sourceProvider;
            this.volume = 1.0f;
        }

        /// <summary>
        /// Reads bytes from this wave stream
        /// </summary>
        /// <param name="buffer">The destination buffer</param>
        /// <returns>Number of bytes read.</returns>
        public int Read(Span<byte> buffer)
        {
            int sourceBytesRequired = buffer.Length * 2;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceBytesRequired);
            int sourceBytesRead = sourceProvider.Read(sourceBuffer.AsSpan(0, sourceBytesRequired));
            var sourceFloatSpan = MemoryMarshal.Cast<byte, float>(sourceBuffer.AsSpan());
            var destShortSpan = MemoryMarshal.Cast<byte, short>(buffer);

            int sourceSamples = sourceBytesRead / 4;
            for (int sample = 0; sample < sourceSamples; sample++)
            {
                // adjust volume
                float sample32 = sourceFloatSpan[sample] * volume;
                // clip
                if (sample32 > 1.0f)
                    sample32 = 1.0f;
                if (sample32 < -1.0f)
                    sample32 = -1.0f;
                destShortSpan[sample] = (short)(sample32 * 32767);
            }

            return sourceSamples * 2;
        }

        /// <summary>
        /// <see cref="IAudioSource.WaveFormat"/>
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
