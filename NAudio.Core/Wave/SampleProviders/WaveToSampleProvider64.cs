using System;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Helper class turning an already 64 bit floating point IWaveProvider
    /// into an ISampleProvider - hopefully not needed for most applications
    /// </summary>
    public class WaveToSampleProvider64 : SampleProviderConverterBase
    {
        /// <summary>
        /// Initializes a new instance of the WaveToSampleProvider class
        /// </summary>
        /// <param name="source">Source wave provider, must be IEEE float</param>
        public WaveToSampleProvider64(IWaveProvider source)
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
        public override int Read(Span<float> buffer)
        {
            int bytesNeeded = buffer.Length * 8;
            EnsureSourceBuffer(bytesNeeded);
            int bytesRead = source.Read(new Span<byte>(sourceBuffer, 0, bytesNeeded));
            int samplesRead = bytesRead / 8;
            int outputIndex = 0;
            for (int n = 0; n < bytesRead; n += 8)
            {
                long sample64 = BitConverter.ToInt64(sourceBuffer, n);
                buffer[outputIndex++] = (float)BitConverter.Int64BitsToDouble(sample64);
            }
            return samplesRead;
        }
    }
}
