using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Wave
{
    /// <summary>
    /// Converts an IWaveProvider containing 16 bit PCM to an
    /// IWaveProviderFloat
    /// </summary>
    public class Pcm16BitToWaveProviderFloat : WaveProviderFloatConverterBase
    {
        /// <summary>
        /// Initialises a new instance of Pcm16BitToWaveProviderFloat
        /// </summary>
        /// <param name="source">Source wave provider</param>
        public Pcm16BitToWaveProviderFloat(IWaveProvider source)
            : base(source)
        {
        }

        /// <summary>
        /// Reads samples from this wave provider
        /// </summary>
        /// <param name="buffer">Sample buffer</param>
        /// <param name="offset">Offset into sample buffer</param>
        /// <param name="count">Samples required</param>
        /// <returns>Number of samples read</returns>
        public override int Read(float[] buffer, int offset, int count)
        {
            int sourceBytesRequired = count * 2;
            EnsureSourceBuffer(sourceBytesRequired);
            int bytesRead = source.Read(sourceBuffer, 0, sourceBytesRequired);
            int outIndex = offset;
            for(int n = 0; n < bytesRead; n+=2)
            {
                buffer[outIndex++] = BitConverter.ToInt16(sourceBuffer, n) / 32768f;
            }
            return bytesRead / 2;
        }
    }
}
