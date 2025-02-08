using System;

#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
#endif

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Silence producing wave provider
    /// Useful for playing silence when doing a WASAPI Loopback Capture
    /// </summary>
    public class SilenceProvider : IWaveProvider
    {
        /// <summary>
        /// Creates a new silence producing wave provider
        /// </summary>
        /// <param name="wf">Desired WaveFormat (should be PCM / IEE float</param>
        public SilenceProvider(WaveFormat wf) { WaveFormat = wf; }

#if NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Read silence into the <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">The array to fill with silence.</param>
        /// <param name="offset">The offset into the given <paramref name="buffer"/> where we will start filling with silence.</param>
        /// <param name="count">The number of bytes in the given <paramref name="buffer"/> to fill with silence.</param>
        /// <exception cref="ArgumentNullException">buffer is null</exception>
        /// <exception cref="IndexOutOfRangeException">
        /// offset is less than the lower bound of array. -or- count is less than zero. -or-
        /// The sum of offset and count is greater than the size of the buffer.
        /// </exception>
        public unsafe int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), $"{nameof(buffer)} is null");
            if (count < 0)
                throw new ArgumentException("Count cannot be less than zero.", nameof(count));
            if (offset < 0)
                throw new ArgumentException("Offset cannot be less than zero.", nameof(offset));
            if (offset + count > buffer.Length)
                throw new IndexOutOfRangeException($"The sum of {nameof(offset)} and {nameof(count)} cannot be greater than the size of the {nameof(buffer)}.");

            fixed (byte* bufferBytes = buffer)
            {
                byte* bufferBytesWithOffset = bufferBytes + offset;
                byte* topAddress = bufferBytesWithOffset + count;

                byte* ptr = bufferBytesWithOffset;

                if (Avx.IsSupported)
                {
                    // We must ensure that the address range we are affecting is a multiple of 32 bytes (256 bits)
                    byte* topAddress256Aligned = topAddress;
                    topAddress256Aligned -= (topAddress - bufferBytesWithOffset) % 32;

                    Vector256<byte> zeroVec = Vector256<byte>.Zero;
                    while (ptr < topAddress256Aligned)
                    {
                        Avx.Store(ptr, zeroVec);
                        ptr += 32;
                    }
                }

                if (Sse2.IsSupported)
                {
                    // We must ensure that the address range we are affecting is a multiple of 16 bytes (128 bits)
                    byte* topAddress128Aligned = topAddress;
                    topAddress128Aligned -= (topAddress - bufferBytesWithOffset) % 16;

                    Vector128<byte> zeroVec = Vector128<byte>.Zero;
                    while (ptr < topAddress128Aligned)
                    {
                        Sse2.Store(ptr, zeroVec);
                        ptr += 16;
                    }
                }

                // Clear any remaining bytes with a standard loop
                while (ptr < topAddress)
                {
                    *ptr = 0;
                    ptr++;
                }
            }

            return count;
        }
#else
        /// <summary>
        /// Read silence into the <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">The array to fill with silence.</param>
        /// <param name="offset">The offset into the given <paramref name="buffer"/> where we will start filling with silence.</param>
        /// <param name="count">The number of bytes in the given <paramref name="buffer"/> to fill with silence.</param>
        public int Read(byte[] buffer, int offset, int count)
        {
            Array.Clear(buffer, offset, count);
            return count;
        }
#endif

        /// <summary>
        /// WaveFormat of this silence producing wave provider
        /// </summary>
        public WaveFormat WaveFormat { get; private set; }
    }
}
