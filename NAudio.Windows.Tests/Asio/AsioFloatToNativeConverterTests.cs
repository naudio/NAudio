using System;
using System.Runtime.InteropServices;
using NAudio.Wave.Asio;
using NUnit.Framework;

namespace NAudioTests.Asio
{
    [TestFixture]
    [Category("UnitTest")]
    public class AsioFloatToNativeConverterTests
    {
        [Test]
        public void Select_ReturnsConverterForEachSupportedFormat()
        {
            Assert.That(AsioFloatToNativeConverter.Select(AsioSampleType.Int16LSB), Is.Not.Null);
            Assert.That(AsioFloatToNativeConverter.Select(AsioSampleType.Int24LSB), Is.Not.Null);
            Assert.That(AsioFloatToNativeConverter.Select(AsioSampleType.Int32LSB), Is.Not.Null);
            Assert.That(AsioFloatToNativeConverter.Select(AsioSampleType.Float32LSB), Is.Not.Null);
        }

        [TestCase(AsioSampleType.Int16MSB)]
        [TestCase(AsioSampleType.Int32MSB)]
        [TestCase(AsioSampleType.Int24MSB)]
        [TestCase(AsioSampleType.Float64LSB)]
        [TestCase(AsioSampleType.DSDInt8LSB1)]
        public void Select_ThrowsForUnsupportedFormats(AsioSampleType unsupported)
        {
            Assert.Throws<NotSupportedException>(() => AsioFloatToNativeConverter.Select(unsupported));
        }

        [Test]
        public void ConvertToInt16Lsb_ScalesAndClamps()
        {
            // Round-trip the supported full-scale points and verify out-of-range floats clamp instead of wrapping.
            var source = new float[] { 0f, 1f, -1f, 0.5f, 1.5f, -1.5f };
            var destination = new short[6];

            ConvertToBytes(AsioSampleType.Int16LSB, source, MemoryMarshal.AsBytes(destination.AsSpan()), 6);

            Assert.That(destination[0], Is.EqualTo((short)0));
            Assert.That(destination[1], Is.EqualTo(short.MaxValue));
            Assert.That(destination[2], Is.EqualTo(-short.MaxValue));
            Assert.That(destination[3], Is.EqualTo((short)(0.5f * short.MaxValue)));
            Assert.That(destination[4], Is.EqualTo(short.MaxValue), "1.5f must clamp to short.MaxValue, not wrap.");
            Assert.That(destination[5], Is.EqualTo(-short.MaxValue), "-1.5f must clamp to -short.MaxValue.");
        }

        [Test]
        public void ConvertToInt32Lsb_ScalesAndClamps()
        {
            var source = new float[] { 0f, 1f, -1f, 0.5f, 1.5f, -1.5f };
            var destination = new int[6];

            ConvertToBytes(AsioSampleType.Int32LSB, source, MemoryMarshal.AsBytes(destination.AsSpan()), 6);

            Assert.That(destination[0], Is.EqualTo(0));
            Assert.That(destination[1], Is.EqualTo(int.MaxValue));
            Assert.That(destination[2], Is.EqualTo(-int.MaxValue));
            Assert.That(destination[3], Is.EqualTo((int)(0.5f * int.MaxValue)).Within(1));
            Assert.That(destination[4], Is.EqualTo(int.MaxValue));
            Assert.That(destination[5], Is.EqualTo(-int.MaxValue));
        }

        [Test]
        public void ConvertToFloat32Lsb_PassesThrough()
        {
            // Float32 native is a memcpy — even out-of-range values are preserved verbatim because the user
            // chose this format specifically to avoid lossy clamping.
            var source = new float[] { 0f, 1f, -1f, 0.25f, 1.5f };
            var destination = new float[5];

            ConvertToBytes(AsioSampleType.Float32LSB, source, MemoryMarshal.AsBytes(destination.AsSpan()), 5);

            Assert.That(destination, Is.EqualTo(source));
        }

        [Test]
        public void ConvertToInt24Lsb_WritesLittleEndianAndSignExtendsCorrectly()
        {
            // Round-trip via the inverse converter — easiest way to verify the 3-byte layout.
            var source = new float[] { 0f, 1f, -1f, 0.5f, 1.5f, -1.5f };
            var nativeBytes = new byte[6 * 3];

            ConvertToBytes(AsioSampleType.Int24LSB, source, nativeBytes.AsSpan(), 6);

            var roundTrip = new float[6];
            ConvertFromBytes(AsioSampleType.Int24LSB, nativeBytes, roundTrip, 6);

            Assert.That(roundTrip[0], Is.EqualTo(0f).Within(1e-6f));
            Assert.That(roundTrip[1], Is.EqualTo(1f).Within(1e-6f), "Full-scale positive must round-trip.");
            Assert.That(roundTrip[2], Is.EqualTo(-1f).Within(1e-6f), "Full-scale negative must round-trip.");
            Assert.That(roundTrip[3], Is.EqualTo(0.5f).Within(1e-6f));
            Assert.That(roundTrip[4], Is.EqualTo(1f).Within(1e-6f), "1.5f must clamp to +1, not wrap negative.");
            Assert.That(roundTrip[5], Is.EqualTo(-1f).Within(1e-6f));
        }

        [TestCase(AsioSampleType.Int16LSB)]
        [TestCase(AsioSampleType.Int24LSB)]
        [TestCase(AsioSampleType.Int32LSB)]
        [TestCase(AsioSampleType.Float32LSB)]
        public void RoundTrip_FloatToNativeAndBack_PreservesSamplesWithinFormatPrecision(AsioSampleType format)
        {
            var source = new float[] { 0f, 0.25f, -0.25f, 0.75f, -0.75f, 0.99f, -0.99f };
            int bytesPerSample = AsioNativeToFloatConverter.BytesPerSample(format);
            var nativeBytes = new byte[source.Length * bytesPerSample];

            ConvertToBytes(format, source, nativeBytes.AsSpan(), source.Length);

            var roundTrip = new float[source.Length];
            ConvertFromBytes(format, nativeBytes, roundTrip, source.Length);

            float tolerance = format == AsioSampleType.Int16LSB ? 1e-4f : 1e-6f;
            for (int i = 0; i < source.Length; i++)
                Assert.That(roundTrip[i], Is.EqualTo(source[i]).Within(tolerance), $"index {i}");
        }

        private static unsafe void ConvertToBytes(AsioSampleType type, float[] source, Span<byte> destinationBytes, int frames)
        {
            var converter = AsioFloatToNativeConverter.Select(type);
            fixed (byte* dst = destinationBytes)
            {
                converter(source.AsSpan(0, frames), new IntPtr(dst), frames);
            }
        }

        private static unsafe void ConvertFromBytes(AsioSampleType type, byte[] sourceBytes, float[] destination, int frames)
        {
            var converter = AsioNativeToFloatConverter.Select(type);
            fixed (byte* src = sourceBytes)
            {
                converter(new IntPtr(src), destination.AsSpan(), frames);
            }
        }
    }
}
