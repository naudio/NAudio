using System;
using System.Runtime.InteropServices;
using NAudio.Wave.Asio;
using NUnit.Framework;

namespace NAudioTests.Asio
{
    [TestFixture]
    [Category("UnitTest")]
    public class AsioNativeToFloatConverterTests
    {
        [Test]
        public void Select_ReturnsConverterForEachSupportedFormat()
        {
            Assert.That(AsioNativeToFloatConverter.Select(AsioSampleType.Int16LSB), Is.Not.Null);
            Assert.That(AsioNativeToFloatConverter.Select(AsioSampleType.Int24LSB), Is.Not.Null);
            Assert.That(AsioNativeToFloatConverter.Select(AsioSampleType.Int32LSB), Is.Not.Null);
            Assert.That(AsioNativeToFloatConverter.Select(AsioSampleType.Float32LSB), Is.Not.Null);
        }

        [TestCase(AsioSampleType.Int16MSB)]
        [TestCase(AsioSampleType.Int32MSB)]
        [TestCase(AsioSampleType.Int24MSB)]
        [TestCase(AsioSampleType.Float64LSB)]
        [TestCase(AsioSampleType.DSDInt8LSB1)]
        public void Select_ThrowsForUnsupportedFormats(AsioSampleType unsupported)
        {
            Assert.Throws<NotSupportedException>(() => AsioNativeToFloatConverter.Select(unsupported));
        }

        [TestCase(AsioSampleType.Int16LSB, 2)]
        [TestCase(AsioSampleType.Int24LSB, 3)]
        [TestCase(AsioSampleType.Int32LSB, 4)]
        [TestCase(AsioSampleType.Float32LSB, 4)]
        public void BytesPerSample_MatchesFormat(AsioSampleType type, int expected)
        {
            Assert.That(AsioNativeToFloatConverter.BytesPerSample(type), Is.EqualTo(expected));
        }

        [Test]
        public void ConvertInt16Lsb_NormalizesSamples()
        {
            var source = new short[] { 0, short.MaxValue, short.MinValue, short.MaxValue / 2 };
            var destination = new float[4];

            Convert(AsioSampleType.Int16LSB, MemoryMarshal.Cast<short, byte>(source).ToArray(), destination, 4);

            Assert.That(destination[0], Is.EqualTo(0f));
            Assert.That(destination[1], Is.EqualTo(1f));
            Assert.That(destination[2], Is.LessThan(-0.9999f).And.GreaterThanOrEqualTo(-1.0001f));
            Assert.That(destination[3], Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void ConvertInt32Lsb_NormalizesSamples()
        {
            var source = new int[] { 0, int.MaxValue, int.MinValue, int.MaxValue / 2 };
            var destination = new float[4];

            Convert(AsioSampleType.Int32LSB, MemoryMarshal.Cast<int, byte>(source).ToArray(), destination, 4);

            Assert.That(destination[0], Is.EqualTo(0f));
            Assert.That(destination[1], Is.EqualTo(1f));
            Assert.That(destination[2], Is.LessThan(-0.9999f).And.GreaterThanOrEqualTo(-1.0001f));
            Assert.That(destination[3], Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void ConvertFloat32Lsb_PassesThrough()
        {
            var source = new float[] { 0f, 1f, -1f, 0.25f, -0.75f };
            var destination = new float[5];

            Convert(AsioSampleType.Float32LSB, MemoryMarshal.Cast<float, byte>(source).ToArray(), destination, 5);

            Assert.That(destination, Is.EqualTo(source));
        }

        [Test]
        public void ConvertInt24Lsb_NormalizesAndSignExtends()
        {
            // 24-bit little-endian samples: 0, +max (0x7FFFFF), -1 (0xFFFFFF), -max (0x800000).
            var sourceBytes = new byte[]
            {
                0x00, 0x00, 0x00,        // 0
                0xFF, 0xFF, 0x7F,        // +(2^23 - 1)
                0xFF, 0xFF, 0xFF,        // -1
                0x00, 0x00, 0x80,        // -2^23
            };
            var destination = new float[4];

            Convert(AsioSampleType.Int24LSB, sourceBytes, destination, 4);

            Assert.That(destination[0], Is.EqualTo(0f));
            Assert.That(destination[1], Is.EqualTo(8388607f / 8388608f).Within(0.0001f));
            Assert.That(destination[2], Is.EqualTo(-1f / 8388608f).Within(0.0001f));
            Assert.That(destination[3], Is.EqualTo(-1f));
        }

        private static unsafe void Convert(AsioSampleType type, byte[] sourceBytes, float[] destination, int frames)
        {
            var converter = AsioNativeToFloatConverter.Select(type);
            fixed (byte* src = sourceBytes)
            {
                converter(new IntPtr(src), destination.AsSpan(), frames);
            }
        }
    }
}
