using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Wave
{
    /// <summary>
    /// Helper class turning an already 32 bit floating point IWaveProvider
    /// into an IWaveProviderFloat - hopefully not needed for most applications
    /// </summary>
    public class WaveProviderToWaveProviderFloat : WaveProviderFloatConverterBase
    {
        /// <summary>
        /// Initializes a new instance of the WaveProviderToWaveProviderFloat class
        /// </summary>
        /// <param name="source">Source wave provider, must be IEEE float</param>
        public WaveProviderToWaveProviderFloat(IWaveProvider source)
            : base(source)
        {
            if (source.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                throw new ArgumentException("Must be already floating point");
            }
        }

        /// <summary>
        /// Reads from this provider
        /// </summary>
        public override int Read(float[] buffer, int offset, int count)
        {
            int bytesNeeded = count * 4;
            EnsureSourceBuffer(bytesNeeded);
            int bytesRead = source.Read(sourceBuffer, 0, bytesNeeded);
            int samplesRead = bytesRead / 4;
            int outputIndex = offset;
            for (int n = 0; n < bytesRead; n+=4)
            {
                buffer[outputIndex++] = BitConverter.ToSingle(sourceBuffer, n);
            }
            return samplesRead;
        }
    }
}
