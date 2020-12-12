using System;
using System.Runtime.InteropServices;
using NAudio.Utils;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Takes a stereo 16 bit input and turns it mono, allowing you to select left or right channel only or mix them together
    /// </summary>
    public class StereoToMonoProvider16 : IWaveProvider
    {
        private readonly IWaveProvider sourceProvider;
        private byte[] sourceBuffer;

        /// <summary>
        /// Creates a new mono waveprovider based on a stereo input
        /// </summary>
        /// <param name="sourceProvider">Stereo 16 bit PCM input</param>
        public StereoToMonoProvider16(IWaveProvider sourceProvider)
        {
            LeftVolume = 0.5f;
            RightVolume = 0.5f;
            if (sourceProvider.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
            {
                throw new ArgumentException("Source must be PCM");
            }
            if (sourceProvider.WaveFormat.Channels != 2)
            {
                throw new ArgumentException("Source must be stereo");
            }
            if (sourceProvider.WaveFormat.BitsPerSample != 16)
            {
                throw new ArgumentException("Source must be 16 bit");
            }
            this.sourceProvider = sourceProvider;
            WaveFormat = new WaveFormat(sourceProvider.WaveFormat.SampleRate, 1);
        }

        /// <summary>
        /// 1.0 to mix the mono source entirely to the left channel
        /// </summary>
        public float LeftVolume { get; set; }

        /// <summary>
        /// 1.0 to mix the mono source entirely to the right channel
        /// </summary>
        public float RightVolume { get; set; }

        /// <summary>
        /// Output Wave Format
        /// </summary>
        public WaveFormat WaveFormat { get; private set; }

        /// <summary>
        /// Reads bytes from this WaveProvider
        /// </summary>
        public int Read(Span<byte> buffer)
        {
            int sourceBytesRequired = buffer.Length * 2;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceBytesRequired);
            var sourceWaveBuffer = MemoryMarshal.Cast<byte, short>(sourceBuffer);
            var destWaveBuffer = MemoryMarshal.Cast<byte, short>(buffer);

            int sourceBytesRead = sourceProvider.Read(new Span<byte>(sourceBuffer, 0, sourceBytesRequired));
            int samplesRead = sourceBytesRead / 2;
            int destOffset = 0 / 2;
            for (int sample = 0; sample < samplesRead; sample+=2)
            {
                short left = sourceWaveBuffer[sample];
                short right = sourceWaveBuffer[sample+1];
                float outSample = (left * LeftVolume) + (right * RightVolume);
                // hard limiting
                if (outSample > Int16.MaxValue) outSample = Int16.MaxValue;
                if (outSample < Int16.MinValue) outSample = Int16.MinValue;

                destWaveBuffer[destOffset++] = (short)outSample;
            }
            return sourceBytesRead / 2;
        }
    }
}
