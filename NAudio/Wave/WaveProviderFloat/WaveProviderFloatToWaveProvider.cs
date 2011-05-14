using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Wave
{
    /// <summary>
    /// Helper class for when you need to convert back to a WaveProvider from
    /// a WaveProviderFloat. Keeps it as IEEE float
    /// </summary>
    public class WaveProviderFloatToWaveProvider : IWaveProvider
    {
        private IWaveProviderFloat source;

        /// <summary>
        /// Initializes a new instance of the WaveProviderFloatToWaveProvider class
        /// </summary>
        /// <param name="source">Source wave provider</param>
        public WaveProviderFloatToWaveProvider(IWaveProviderFloat source)
        {
            if (source.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                throw new ArgumentException("Must be already floating point");
            }
            this.source = source;
        }

        /// <summary>
        /// Reads from this provider
        /// </summary>
        public int Read(byte[] buffer, int offset, int count)
        {
            int samplesNeeded = count / 4;
            WaveBuffer wb = new WaveBuffer(buffer);
            int samplesRead = source.Read(wb.FloatBuffer, offset / 4, samplesNeeded);
            return samplesRead * 4;
        }

        /// <summary>
        /// The waveformat of this WaveProvider (same as the source)
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return source.WaveFormat; }
        }
    }
}
