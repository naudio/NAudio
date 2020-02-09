namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Converts an IWaveProvider containing 32 bit PCM to an
    /// ISampleProvider
    /// </summary>
    public class Pcm32BitToSampleProvider : SampleProviderConverterBase
    {
        /// <summary>
        /// Initialises a new instance of Pcm32BitToSampleProvider
        /// </summary>
        /// <param name="source">Source Wave Provider</param>
        public Pcm32BitToSampleProvider(IWaveProvider source)
            : base(source)
        {

        }

        /// <summary>
        /// Reads floating point samples from this sample provider
        /// </summary>
        /// <param name="buffer">sample buffer</param>
        /// <param name="offset">offset within sample buffer to write to</param>
        /// <param name="count">number of samples required</param>
        /// <returns>number of samples provided</returns>
        public override int Read(float[] buffer, int offset, int count)
        {
            int sourceBytesRequired = count*4;
            EnsureSourceBuffer(sourceBytesRequired);
            int bytesRead = source.Read(sourceBuffer, 0, sourceBytesRequired);
            int outIndex = offset;
            for (int n = 0; n < bytesRead; n += 4)
            {
                buffer[outIndex++] = (((sbyte) sourceBuffer[n + 3] << 24 |
                                       sourceBuffer[n + 2] << 16) |
                                      (sourceBuffer[n + 1] << 8) |
                                      sourceBuffer[n])/2147483648f;
            }
            return bytesRead/4;
        }
    }
}
