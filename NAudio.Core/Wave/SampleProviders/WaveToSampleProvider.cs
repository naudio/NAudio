using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Helper class turning an already 32 bit floating point IAudioSource
    /// into an ISampleSource - hopefully not needed for most applications
    /// </summary>
    public class WaveToSampleProvider : SampleProviderConverterBase
    {
        /// <summary>
        /// Initializes a new instance of the WaveToSampleProvider class
        /// </summary>
        /// <param name="source">Source wave provider, must be IEEE float</param>
        public WaveToSampleProvider(IAudioSource source)
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
            int bytesNeeded = buffer.Length * 4;
            EnsureSourceBuffer(bytesNeeded);
            int bytesRead = source.Read(sourceBuffer.AsSpan(0, bytesNeeded));
            int samplesRead = bytesRead / 4;
            var floatSpan = MemoryMarshal.Cast<byte, float>(sourceBuffer.AsSpan(0, bytesRead));
            floatSpan.Slice(0, samplesRead).CopyTo(buffer);
            return samplesRead;
        }
    }
}
