using System;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Converts an IAudioSource containing 16 bit PCM to an
    /// ISampleSource
    /// </summary>
    public class Pcm16BitToSampleProvider : SampleProviderConverterBase
    {
        /// <summary>
        /// Initialises a new instance of Pcm16BitToSampleProvider
        /// </summary>
        /// <param name="source">Source wave provider</param>
        public Pcm16BitToSampleProvider(IAudioSource source)
            : base(source)
        {
        }

        /// <summary>
        /// Reads samples from this sample provider
        /// </summary>
        /// <param name="buffer">Sample buffer</param>
        /// <returns>Number of samples read</returns>
        public override int Read(Span<float> buffer)
        {
            int sourceBytesRequired = buffer.Length * 2;
            EnsureSourceBuffer(sourceBytesRequired);
            int bytesRead = source.Read(sourceBuffer.AsSpan(0, sourceBytesRequired));
            int outIndex = 0;
            for(int n = 0; n < bytesRead; n+=2)
            {
                buffer[outIndex++] = BitConverter.ToInt16(sourceBuffer, n) / 32768f;
            }
            return bytesRead / 2;
        }
    }
}
