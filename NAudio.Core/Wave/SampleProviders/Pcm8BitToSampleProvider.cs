using System;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Converts an IAudioSource containing 8 bit PCM to an
    /// ISampleSource
    /// </summary>
    public class Pcm8BitToSampleProvider : SampleProviderConverterBase
    {
        /// <summary>
        /// Initialises a new instance of Pcm8BitToSampleProvider
        /// </summary>
        /// <param name="source">Source wave provider</param>
        public Pcm8BitToSampleProvider(IAudioSource source) :
            base(source)
        {
        }

        /// <summary>
        /// Reads samples from this sample provider
        /// </summary>
        /// <param name="buffer">Sample buffer</param>
        /// <returns>Number of samples read</returns>
        public override int Read(Span<float> buffer)
        {
            int sourceBytesRequired = buffer.Length;
            EnsureSourceBuffer(sourceBytesRequired);
            int bytesRead = source.Read(sourceBuffer.AsSpan(0, sourceBytesRequired));
            int outIndex = 0;
            for (int n = 0; n < bytesRead; n++)
            {
                buffer[outIndex++] = sourceBuffer[n] / 128f - 1.0f;
            }
            return bytesRead;
        }
    }
}
