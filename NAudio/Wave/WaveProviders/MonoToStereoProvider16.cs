using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Utils;

namespace NAudio.Wave
{
    /// <summary>
    /// Converts from mono to stereo, allowing freedom to route all, some, or none of the incoming signal to left or right channels
    /// </summary>
    public class MonoToStereoProvider16 : IWaveProvider
    {
        private IWaveProvider sourceProvider;
        private WaveFormat outputFormat;
        private byte[] sourceBuffer;

        /// <summary>
        /// Creates a new stereo waveprovider based on a mono input
        /// </summary>
        /// <param name="sourceProvider">Mono 16 bit PCM input</param>
        public MonoToStereoProvider16(IWaveProvider sourceProvider)
        {
            if (sourceProvider.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
            {
                throw new ArgumentException("Source must be PCM");
            }
            if (sourceProvider.WaveFormat.Channels != 1)
            {
                throw new ArgumentException("Source must be Mono");
            }
            if (sourceProvider.WaveFormat.BitsPerSample != 16)
            {
                throw new ArgumentException("Source must be 16 bit");
            }
            this.sourceProvider = sourceProvider;
            this.outputFormat = new WaveFormat(sourceProvider.WaveFormat.SampleRate, 2);
            RightVolume = 1.0f;
            LeftVolume = 1.0f;
        }

        /// <summary>
        /// 1.0 to copy the mono stream to the left channel without adjusting volume
        /// </summary>
        public float LeftVolume { get; set; }

        /// <summary>
        /// 1.0 to copy the mono stream to the right channel without adjusting volume
        /// </summary>
        public float RightVolume { get; set; }

        /// <summary>
        /// Output Wave Format
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return this.outputFormat; }
        }

        /// <summary>
        /// Reads bytes from this WaveProvider
        /// </summary>
        public int Read(byte[] buffer, int offset, int count)
        {            
            int sourceBytesRequired = count / 2;
            this.sourceBuffer = BufferHelpers.Ensure(this.sourceBuffer, sourceBytesRequired);
            WaveBuffer sourceWaveBuffer = new WaveBuffer(sourceBuffer);
            WaveBuffer destWaveBuffer = new WaveBuffer(buffer);

            int sourceBytesRead = sourceProvider.Read(sourceBuffer, 0, sourceBytesRequired);
            int samplesRead = sourceBytesRead / 2;
            int destOffset = offset / 2;
            for (int sample = 0; sample < samplesRead; sample++)
            {
                short sampleVal = sourceWaveBuffer.ShortBuffer[sample];
                destWaveBuffer.ShortBuffer[destOffset++] = (short)(LeftVolume * sampleVal);
                destWaveBuffer.ShortBuffer[destOffset++] = (short)(RightVolume * sampleVal);
            }
            return samplesRead * 4;
        }

    }
}
