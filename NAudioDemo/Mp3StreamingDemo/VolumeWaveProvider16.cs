using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace NAudioDemo
{
    /// <summary>
    /// Helper class allowing us to modify the volume of a 16 bit stream.
    /// Warning - no clipping protection, so do not amplify
    /// </summary>
    class VolumeWaveProvider16 : IWaveProvider
    {
        /// <summary>
        /// Gets or sets volume. 1.0 is full scale.
        /// </summary>
        public float Volume { get; set; }
        private IWaveProvider sourceProvider;

        public VolumeWaveProvider16(IWaveProvider sourceProvider)
        {
            this.sourceProvider = sourceProvider;
            if (sourceProvider.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
                throw new ArgumentException("Expecting PCM input");
            if (sourceProvider.WaveFormat.BitsPerSample != 16)
                throw new ArgumentException("Expecting 16 bit");
        }

        public WaveFormat WaveFormat
        {
            get { return sourceProvider.WaveFormat; }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            // always read from the source
            int bytesRead = sourceProvider.Read(buffer, offset, count);
            if (this.Volume == 0.0f)
            {
                Array.Clear(buffer, offset, bytesRead);
            }
            else if (this.Volume != 1.0f)
            {
                for (int n = 0; n < bytesRead; n+=2)
                {
                    short sample = (short)((buffer[n + 1] << 8) | buffer[n + 0]);
                    // n.b. no clipping test going on here
                    sample = (short)(sample * this.Volume);
                    buffer[n] = (byte)(sample & 0xFF);
                    buffer[n+1] = (byte)(sample >> 8);
                }
            }
            return bytesRead;
        }
    }
}
