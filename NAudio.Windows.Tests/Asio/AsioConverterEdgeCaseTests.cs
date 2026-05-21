using System;
using System.Runtime.InteropServices;
using NAudio.Wave.Asio;
using NUnit.Framework;

namespace NAudioTests.Asio
{
    /// <summary>
    /// Edge-case coverage for <see cref="AsioFloatToNativeConverter"/> and <see cref="AsioNativeToFloatConverter"/>:
    /// NaN/Inf inputs, frame=0 buffers, Int24 sign-extension across the full negative range. The happy-path
    /// conversion math is covered by <c>AsioFloatToNativeConverterTests</c> / <c>AsioNativeToFloatConverterTests</c>.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class AsioConverterEdgeCaseTests
    {
        // -- frame=0 must be a clean no-op -----------------------------------------------------------------------

        [TestCase(AsioSampleType.Int16LSB)]
        [TestCase(AsioSampleType.Int24LSB)]
        [TestCase(AsioSampleType.Int32LSB)]
        [TestCase(AsioSampleType.Float32LSB)]
        public void FloatToNative_FrameCountZero_DoesNotTouchBuffers(AsioSampleType format)
        {
            int bytesPerSample = AsioNativeToFloatConverter.BytesPerSample(format);
            var nativeBytes = new byte[bytesPerSample * 4];
            // Pre-fill the destination with a sentinel pattern; a frame=0 call must not modify a single byte.
            for (int i = 0; i < nativeBytes.Length; i++) nativeBytes[i] = 0xAB;
            var source = new float[] { 1f, -1f, 0.5f, 0f };

            ConvertToBytes(format, source, nativeBytes.AsSpan(), frames: 0);

            for (int i = 0; i < nativeBytes.Length; i++)
                Assert.That(nativeBytes[i], Is.EqualTo((byte)0xAB), $"byte {i} was modified");
        }

        [TestCase(AsioSampleType.Int16LSB)]
        [TestCase(AsioSampleType.Int24LSB)]
        [TestCase(AsioSampleType.Int32LSB)]
        [TestCase(AsioSampleType.Float32LSB)]
        public void NativeToFloat_FrameCountZero_DoesNotTouchBuffers(AsioSampleType format)
        {
            int bytesPerSample = AsioNativeToFloatConverter.BytesPerSample(format);
            var nativeBytes = new byte[bytesPerSample * 4];
            // Pre-fill the destination with a recognisable sentinel; a frame=0 call must not overwrite it.
            var destination = new float[] { 7f, 8f, 9f, 10f };

            ConvertFromBytes(format, nativeBytes, destination, frames: 0);

            Assert.That(destination, Is.EqualTo(new[] { 7f, 8f, 9f, 10f }));
        }

        // -- Float-to-native: Inf clamps; NaN must not throw or corrupt unrelated samples -----------------------

        [Test]
        public void FloatToNative_Int16Lsb_ClampsPositiveInfinity()
        {
            var source = new float[] { float.PositiveInfinity };
            var destination = new short[1];
            ConvertToBytes(AsioSampleType.Int16LSB, source, MemoryMarshal.AsBytes(destination.AsSpan()), 1);
            Assert.That(destination[0], Is.EqualTo(short.MaxValue));
        }

        [Test]
        public void FloatToNative_Int16Lsb_ClampsNegativeInfinity()
        {
            var source = new float[] { float.NegativeInfinity };
            var destination = new short[1];
            ConvertToBytes(AsioSampleType.Int16LSB, source, MemoryMarshal.AsBytes(destination.AsSpan()), 1);
            Assert.That(destination[0], Is.EqualTo(-short.MaxValue));
        }

        [Test]
        public void FloatToNative_Int32Lsb_ClampsPositiveInfinity()
        {
            var source = new float[] { float.PositiveInfinity };
            var destination = new int[1];
            ConvertToBytes(AsioSampleType.Int32LSB, source, MemoryMarshal.AsBytes(destination.AsSpan()), 1);
            Assert.That(destination[0], Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void FloatToNative_Int32Lsb_ClampsNegativeInfinity()
        {
            var source = new float[] { float.NegativeInfinity };
            var destination = new int[1];
            ConvertToBytes(AsioSampleType.Int32LSB, source, MemoryMarshal.AsBytes(destination.AsSpan()), 1);
            Assert.That(destination[0], Is.EqualTo(-int.MaxValue));
        }

        [Test]
        public void FloatToNative_Int24Lsb_ClampsInfinities()
        {
            var source = new float[] { float.PositiveInfinity, float.NegativeInfinity };
            var bytes = new byte[6];
            ConvertToBytes(AsioSampleType.Int24LSB, source, bytes.AsSpan(), 2);

            int sample0 = bytes[0] | (bytes[1] << 8) | ((sbyte)bytes[2] << 16);
            int sample1 = bytes[3] | (bytes[4] << 8) | ((sbyte)bytes[5] << 16);
            Assert.That(sample0, Is.EqualTo(8388607));   // 2^23 - 1
            Assert.That(sample1, Is.EqualTo(-8388607));  // -(2^23 - 1)
        }

        [Test]
        public void FloatToNative_Float32Lsb_PreservesInfinities()
        {
            // Float32 is a memcpy — Inf must pass through untouched.
            var source = new float[] { float.PositiveInfinity, float.NegativeInfinity };
            var destination = new float[2];
            ConvertToBytes(AsioSampleType.Float32LSB, source, MemoryMarshal.AsBytes(destination.AsSpan()), 2);
            Assert.That(destination[0], Is.EqualTo(float.PositiveInfinity));
            Assert.That(destination[1], Is.EqualTo(float.NegativeInfinity));
        }

        [TestCase(AsioSampleType.Int16LSB)]
        [TestCase(AsioSampleType.Int24LSB)]
        [TestCase(AsioSampleType.Int32LSB)]
        public void FloatToNative_NaN_DoesNotThrowAndDoesNotCorruptNeighbouringSamples(AsioSampleType format)
        {
            // NaN comparisons return false, so the clamp path doesn't catch them; the int cast of NaN is
            // implementation-defined but in .NET produces 0. We don't pin the exact NaN-mapping value — we just
            // require it to (a) not throw and (b) not damage the surrounding well-formed samples.
            int bps = AsioNativeToFloatConverter.BytesPerSample(format);
            var source = new float[] { 0.5f, float.NaN, -0.5f };
            var nativeBytes = new byte[bps * 3];

            Assert.DoesNotThrow(() => ConvertToBytes(format, source, nativeBytes.AsSpan(), 3));

            // Round-trip the surrounding samples and assert they survived.
            var roundTrip = new float[3];
            ConvertFromBytes(format, nativeBytes, roundTrip, 3);
            float tolerance = format == AsioSampleType.Int16LSB ? 1e-4f : 1e-6f;
            Assert.That(roundTrip[0], Is.EqualTo(0.5f).Within(tolerance));
            Assert.That(roundTrip[2], Is.EqualTo(-0.5f).Within(tolerance));
        }

        [Test]
        public void FloatToNative_Float32Lsb_PreservesNaN()
        {
            var source = new float[] { float.NaN };
            var destination = new float[1];
            ConvertToBytes(AsioSampleType.Float32LSB, source, MemoryMarshal.AsBytes(destination.AsSpan()), 1);
            Assert.That(float.IsNaN(destination[0]), Is.True);
        }

        // -- Native-to-float: Float32 path preserves Inf/NaN ----------------------------------------------------

        [Test]
        public void NativeToFloat_Float32Lsb_PreservesInfinitiesAndNaN()
        {
            var nativeFloats = new float[] { float.PositiveInfinity, float.NegativeInfinity, float.NaN };
            var nativeBytes = MemoryMarshal.Cast<float, byte>(nativeFloats).ToArray();
            var destination = new float[3];

            ConvertFromBytes(AsioSampleType.Float32LSB, nativeBytes, destination, 3);

            Assert.That(destination[0], Is.EqualTo(float.PositiveInfinity));
            Assert.That(destination[1], Is.EqualTo(float.NegativeInfinity));
            Assert.That(float.IsNaN(destination[2]), Is.True);
        }

        // -- Int24 sign extension across a wider range than the existing happy-path test ------------------------

        [Test]
        public void NativeToFloat_Int24Lsb_SignExtendsAcrossPositiveAndNegativeRange()
        {
            // Six points sweeping the 24-bit range; verifies the (sbyte)src[2] sign-extension on the high byte
            // produces correct floats both above and below zero, and at non-trivial magnitudes.
            var samples = new int[]
            {
                0,                  // 0x000000
                1,                  // 0x000001 — smallest positive
                -1,                 // 0xFFFFFF — needs sign extension
                4194304,            // 0x400000 — half-scale positive
                -4194304,           // 0xC00000 — half-scale negative
                -8388607,           // 0x800001 — one above the symmetric floor (intentionally avoids the asymmetric -2^23)
            };
            var nativeBytes = new byte[samples.Length * 3];
            for (int i = 0; i < samples.Length; i++)
            {
                int s = samples[i];
                nativeBytes[i * 3 + 0] = (byte)s;
                nativeBytes[i * 3 + 1] = (byte)(s >> 8);
                nativeBytes[i * 3 + 2] = (byte)(s >> 16);
            }

            var destination = new float[samples.Length];
            ConvertFromBytes(AsioSampleType.Int24LSB, nativeBytes, destination, samples.Length);

            const float scale = 1f / 8388608f;
            for (int i = 0; i < samples.Length; i++)
                Assert.That(destination[i], Is.EqualTo(samples[i] * scale).Within(1e-7f),
                    $"sample {i} (raw 0x{samples[i] & 0xFFFFFF:X6})");
        }

        [Test]
        public void NativeToFloat_Int24Lsb_AsymmetricFloorMapsToMinusOne()
        {
            // Documented asymmetry: the input convention normalizes by 2^23, so 0x800000 (-2^23) maps exactly to -1f
            // — the symmetric counterpart of a clamped +1f doesn't exist on the input side. This test pins that.
            var nativeBytes = new byte[] { 0x00, 0x00, 0x80 };
            var destination = new float[1];
            ConvertFromBytes(AsioSampleType.Int24LSB, nativeBytes, destination, 1);
            Assert.That(destination[0], Is.EqualTo(-1f));
        }

        // -- helpers ---------------------------------------------------------------------------------------------

        private static unsafe void ConvertToBytes(AsioSampleType type, float[] source, Span<byte> destinationBytes, int frames)
        {
            var converter = AsioFloatToNativeConverter.Select(type);
            fixed (byte* dst = destinationBytes)
            {
                converter(source.AsSpan(0, source.Length), new IntPtr(dst), frames);
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
