using System;

namespace NAudio.Wave.Asio
{
    /// <summary>
    /// Converts a single channel of native ASIO samples into a <see cref="Span{Single}"/> of normalized floats in [-1.0, +1.0].
    /// Supported native formats are <c>Int16LSB</c>, <c>Int24LSB</c>, <c>Int32LSB</c>, and <c>Float32LSB</c> — the four
    /// formats used by the vast majority of ASIO drivers. Other formats throw <see cref="NotSupportedException"/>.
    /// </summary>
    /// <remarks>
    /// This is exposed publicly so callers using the raw-buffer escape hatch on <see cref="NAudio.Wave.AsioDevice"/>
    /// (<c>RawInput</c>/<c>RawOutput</c>) can convert bytes to float without re-implementing the per-format math.
    /// </remarks>
    public static class AsioNativeToFloatConverter
    {
        /// <summary>
        /// Converts <paramref name="frames"/> samples from <paramref name="source"/> (driver-owned native memory)
        /// into normalized floats written into <paramref name="destination"/>.
        /// </summary>
        public delegate void ConverterFn(IntPtr source, Span<float> destination, int frames);

        /// <summary>
        /// Returns a converter for the given native ASIO sample type, or throws <see cref="NotSupportedException"/>
        /// if the format is not supported by the new <see cref="AsioDevice"/> API.
        /// </summary>
        public static ConverterFn Select(AsioSampleType nativeFormat) => nativeFormat switch
        {
            AsioSampleType.Int16LSB => ConvertInt16Lsb,
            AsioSampleType.Int24LSB => ConvertInt24Lsb,
            AsioSampleType.Int32LSB => ConvertInt32Lsb,
            AsioSampleType.Float32LSB => ConvertFloat32Lsb,
            _ => throw new NotSupportedException(
                $"ASIO sample type {nativeFormat} is not supported. " +
                "Supported native formats: Int16LSB, Int24LSB, Int32LSB, Float32LSB.")
        };

        /// <summary>
        /// Bytes per sample for the given native format. Throws for unsupported formats.
        /// </summary>
        public static int BytesPerSample(AsioSampleType nativeFormat) => nativeFormat switch
        {
            AsioSampleType.Int16LSB => 2,
            AsioSampleType.Int24LSB => 3,
            AsioSampleType.Int32LSB => 4,
            AsioSampleType.Float32LSB => 4,
            _ => throw new NotSupportedException(
                $"ASIO sample type {nativeFormat} is not supported.")
        };

        private static unsafe void ConvertInt16Lsb(IntPtr source, Span<float> destination, int frames)
        {
            var src = (short*)source;
            const float scale = 1f / short.MaxValue;
            for (int i = 0; i < frames; i++)
            {
                destination[i] = src[i] * scale;
            }
        }

        private static unsafe void ConvertInt24Lsb(IntPtr source, Span<float> destination, int frames)
        {
            var src = (byte*)source;
            const float scale = 1f / 8388608f; // 2^23
            for (int i = 0; i < frames; i++)
            {
                // Sign-extended 24-bit little-endian to int32.
                int sample = src[0] | (src[1] << 8) | ((sbyte)src[2] << 16);
                destination[i] = sample * scale;
                src += 3;
            }
        }

        private static unsafe void ConvertInt32Lsb(IntPtr source, Span<float> destination, int frames)
        {
            var src = (int*)source;
            const float scale = 1f / int.MaxValue;
            for (int i = 0; i < frames; i++)
            {
                destination[i] = src[i] * scale;
            }
        }

        private static unsafe void ConvertFloat32Lsb(IntPtr source, Span<float> destination, int frames)
        {
            var src = new ReadOnlySpan<float>((void*)source, frames);
            src.CopyTo(destination);
        }
    }
}
