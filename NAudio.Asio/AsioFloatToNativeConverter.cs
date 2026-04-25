using System;

namespace NAudio.Wave.Asio
{
    /// <summary>
    /// Converts a single channel of normalized float samples in [-1.0, +1.0] into the driver's native
    /// ASIO sample format. The mirror of <see cref="AsioNativeToFloatConverter"/>; used by the duplex output path
    /// of <see cref="NAudio.Wave.AsioDevice"/>.
    /// </summary>
    /// <remarks>
    /// Supported native formats are <c>Int16LSB</c>, <c>Int24LSB</c>, <c>Int32LSB</c>, and <c>Float32LSB</c>.
    /// Float inputs outside [-1.0, +1.0] are clamped to the format's full-scale value to avoid integer wrap.
    /// </remarks>
    public static class AsioFloatToNativeConverter
    {
        /// <summary>
        /// Converts <paramref name="frames"/> samples from <paramref name="source"/> into native bytes
        /// written into <paramref name="destination"/> (driver-owned native memory).
        /// </summary>
        public delegate void ConverterFn(ReadOnlySpan<float> source, IntPtr destination, int frames);

        /// <summary>
        /// Returns a converter for the given native ASIO sample type, or throws <see cref="NotSupportedException"/>
        /// if the format is not supported by the new <see cref="AsioDevice"/> API.
        /// </summary>
        public static ConverterFn Select(AsioSampleType nativeFormat) => nativeFormat switch
        {
            AsioSampleType.Int16LSB => ConvertToInt16Lsb,
            AsioSampleType.Int24LSB => ConvertToInt24Lsb,
            AsioSampleType.Int32LSB => ConvertToInt32Lsb,
            AsioSampleType.Float32LSB => ConvertToFloat32Lsb,
            _ => throw new NotSupportedException(
                $"ASIO sample type {nativeFormat} is not supported. " +
                "Supported native formats: Int16LSB, Int24LSB, Int32LSB, Float32LSB.")
        };

        private static unsafe void ConvertToInt16Lsb(ReadOnlySpan<float> source, IntPtr destination, int frames)
        {
            var dst = (short*)destination;
            for (int i = 0; i < frames; i++)
            {
                float s = source[i];
                if (s > 1f) s = 1f; else if (s < -1f) s = -1f;
                dst[i] = (short)(s * short.MaxValue);
            }
        }

        private static unsafe void ConvertToInt24Lsb(ReadOnlySpan<float> source, IntPtr destination, int frames)
        {
            var dst = (byte*)destination;
            // Standard audio convention: scale by 2^23 - 1 on write so a clamped +1.0f maps to the largest
            // representable positive Int24 (0x7FFFFF) and -1.0f maps to -0x7FFFFF, leaving the asymmetric
            // -0x800000 unused. The Native→Float mirror in AsioNativeToFloatConverter normalizes by 2^23
            // (the symmetric range), so a round-trip of 0x7FFFFF returns slightly under 1.0f — this asymmetry
            // is intentional and matches CoreAudio / JUCE / libsndfile.
            const float scale = 8388607f; // 2^23 - 1
            for (int i = 0; i < frames; i++)
            {
                float s = source[i];
                if (s > 1f) s = 1f; else if (s < -1f) s = -1f;
                int sample = (int)(s * scale);
                dst[0] = (byte)sample;
                dst[1] = (byte)(sample >> 8);
                dst[2] = (byte)(sample >> 16);
                dst += 3;
            }
        }

        private static unsafe void ConvertToInt32Lsb(ReadOnlySpan<float> source, IntPtr destination, int frames)
        {
            var dst = (int*)destination;
            for (int i = 0; i < frames; i++)
            {
                float s = source[i];
                if (s > 1f) s = 1f; else if (s < -1f) s = -1f;
                // float can't represent int.MaxValue exactly (rounds up to 2^31), so 1f*MaxValue and -1f*MaxValue
                // both produce values that overflow a signed 32-bit cast. Stage through long and clamp symmetrically.
                long scaled = (long)(s * (float)int.MaxValue);
                if (scaled > int.MaxValue) scaled = int.MaxValue;
                else if (scaled < -int.MaxValue) scaled = -int.MaxValue;
                dst[i] = (int)scaled;
            }
        }

        private static unsafe void ConvertToFloat32Lsb(ReadOnlySpan<float> source, IntPtr destination, int frames)
        {
            var dst = new Span<float>((void*)destination, frames);
            source.Slice(0, frames).CopyTo(dst);
        }
    }
}
