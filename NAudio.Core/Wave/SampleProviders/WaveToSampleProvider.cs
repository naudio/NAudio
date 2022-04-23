using System;
#if CPU_INTRINSICS
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Helper class turning an already 32 bit floating point IWaveProvider
    /// into an ISampleProvider - hopefully not needed for most applications
    /// </summary>
    public class WaveToSampleProvider : SampleProviderConverterBase
    {
        /// <summary>
        /// Initializes a new instance of the WaveToSampleProvider class
        /// </summary>
        /// <param name="source">Source wave provider, must be IEEE float</param>
        public WaveToSampleProvider(IWaveProvider source)
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


            unsafe
            {
#if CPU_INTRINSICS
                fixed (byte* pBytes = sourceBuffer)
                fixed (float* pFloat = buffer)
                {
                    var pFloatStart = pFloat + offset;
                    var pFloatCurrent = pFloatStart;
                    var pFloatEnd = pFloatStart;
                    var pBytesCurrent = pBytes;

                    if (Avx.IsSupported)
                    {
                        var vector256SampleCount = count & ~7;
                        pFloatEnd = pFloatStart + vector256SampleCount;
                        while (pFloatCurrent < pFloatEnd)
                        {
                            var input = Avx.LoadVector256(pBytesCurrent).AsSingle();
                            Avx.Store(pFloatCurrent, input);
                            pFloatCurrent += 8;
                            pBytesCurrent += 32;
                        }
                    }

                    pFloatEnd = pFloatStart + count;
                    while (pFloatCurrent < pFloatEnd)
                    {
                        *pFloatCurrent = *(float*)pBytesCurrent;
                        pFloatCurrent++;
                        pBytesCurrent += 4;
                    }
                }
#else
                fixed (byte* pBytes = sourceBuffer)
                {
                    float* pFloat = (float*)pBytes;
                    for (int n = 0, i = 0; n < bytesRead; n += 4, i++)
                    {
                        buffer[outputIndex++] = *(pFloat + i);
                    }
                }
#endif
            }
            return samplesRead;
        }
    }
}
