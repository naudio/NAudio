using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using NAudio.Wave;
using NAudio.Wave.Asio;
using NUnit.Framework;

namespace NAudioTests.Asio
{
    [TestFixture]
    [Category("UnitTest")]
    public class AsioSampleConvertorTests
    {
        private static readonly Type ConvertorType = typeof(AsioSampleType).Assembly.GetType("NAudio.Wave.Asio.AsioSampleConvertor", throwOnError: true);

        [TestCase(AsioSampleType.Int32LSB, 16, false, 2, "ConvertorShortToInt2Channels")]
        [TestCase(AsioSampleType.Int32LSB, 16, false, 3, "ConvertorShortToIntGeneric")]
        [TestCase(AsioSampleType.Int32LSB, 32, false, 2, "ConvertorIntToInt2Channels")]
        [TestCase(AsioSampleType.Int32LSB, 32, true, 3, "ConvertorFloatToIntGeneric")]
        [TestCase(AsioSampleType.Int16LSB, 16, false, 2, "ConvertorShortToShort2Channels")]
        [TestCase(AsioSampleType.Int16LSB, 32, true, 3, "ConvertorFloatToShortGeneric")]
        [TestCase(AsioSampleType.Int16LSB, 32, false, 3, "ConvertorIntToShortGeneric")]
        [TestCase(AsioSampleType.Int24LSB, 32, true, 2, "ConverterFloatTo24LSBGeneric")]
        [TestCase(AsioSampleType.Float32LSB, 32, true, 3, "ConverterFloatToFloatGeneric")]
        [TestCase(AsioSampleType.Float32LSB, 32, false, 3, "ConvertorIntToFloatGeneric")]
        public void SelectSampleConvertor_ReturnsExpectedMethod(AsioSampleType asioType, int bitsPerSample, bool ieeeFloat, int channels, string expectedMethod)
        {
            var waveFormat = ieeeFloat
                ? WaveFormat.CreateIeeeFloatWaveFormat(48000, channels)
                : new WaveFormat(48000, bitsPerSample, channels);

            var convertor = SelectSampleConvertor(waveFormat, asioType);

            Assert.That(convertor.Method.Name, Is.EqualTo(expectedMethod));
        }

        [Test]
        public void SelectSampleConvertor_ThrowsForUnsupportedAsioType()
        {
            var waveFormat = new WaveFormat(48000, 16, 2);
            Assert.Throws<TargetInvocationException>(() => SelectSampleConvertor(waveFormat, AsioSampleType.Int32MSB));
        }

        [Test]
        public void ConvertorShortToInt2Channels_DeinterleavesAndScales()
        {
            short[] input = { -32768, 32767, 1, -1 };
            int[] left = new int[2];
            int[] right = new int[2];

            var convertor = SelectSampleConvertor(new WaveFormat(48000, 16, 2), AsioSampleType.Int32LSB);
            InvokeConvertor(convertor, input, new Array[] { left, right }, 2, 2);

            Assert.That(left, Is.EqualTo(new[] { -2147483648, 65536 }));
            Assert.That(right, Is.EqualTo(new[] { 2147418112, -65536 }));
        }

        [Test]
        public void ConvertorShortToIntGeneric_DeinterleavesAndScales()
        {
            short[] input = { 1, 2, 3, 4, 5, 6 };
            int[] c0 = new int[2];
            int[] c1 = new int[2];
            int[] c2 = new int[2];

            var convertor = SelectSampleConvertor(new WaveFormat(48000, 16, 3), AsioSampleType.Int32LSB);
            InvokeConvertor(convertor, input, new Array[] { c0, c1, c2 }, 3, 2);

            Assert.That(c0, Is.EqualTo(new[] { 65536, 262144 }));
            Assert.That(c1, Is.EqualTo(new[] { 131072, 327680 }));
            Assert.That(c2, Is.EqualTo(new[] { 196608, 393216 }));
        }

        [Test]
        public void ConvertorFloatToInt2Channels_ClampsAndConverts()
        {
            float[] input = { -2f, 2f, 0.5f, -0.5f };
            int[] left = new int[2];
            int[] right = new int[2];

            var convertor = SelectSampleConvertor(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2), AsioSampleType.Int32LSB);
            InvokeConvertor(convertor, input, new Array[] { left, right }, 2, 2);

            Assert.That(left, Is.EqualTo(new[] { -2147483647, 1073741823 }));
            Assert.That(right, Is.EqualTo(new[] { 2147483647, -1073741823 }));
        }

        [Test]
        public void ConvertorIntToIntGeneric_Deinterleaves()
        {
            int[] input = { 11, 12, 13, 21, 22, 23 };
            int[] c0 = new int[2];
            int[] c1 = new int[2];
            int[] c2 = new int[2];

            var convertor = SelectSampleConvertor(new WaveFormat(48000, 32, 3), AsioSampleType.Int32LSB);
            InvokeConvertor(convertor, input, new Array[] { c0, c1, c2 }, 3, 2);

            Assert.That(c0, Is.EqualTo(new[] { 11, 21 }));
            Assert.That(c1, Is.EqualTo(new[] { 12, 22 }));
            Assert.That(c2, Is.EqualTo(new[] { 13, 23 }));
        }

        [Test]
        public void ConvertorIntToShort2Channels_DownsamplesByTop16Bits()
        {
            int[] input = { 65536, -65536, 131072, -131072 };
            short[] left = new short[2];
            short[] right = new short[2];

            var convertor = SelectSampleConvertor(new WaveFormat(48000, 32, 2), AsioSampleType.Int16LSB);
            InvokeConvertor(convertor, input, new Array[] { left, right }, 2, 2);

            Assert.That(left, Is.EqualTo(new short[] { 1, 2 }));
            Assert.That(right, Is.EqualTo(new short[] { -1, -2 }));
        }

        [Test]
        public void ConvertorIntToShortGeneric_DownsamplesByTop16Bits()
        {
            int[] input = { 65536, -65536, 131072, 196608, -196608, 262144 };
            short[] c0 = new short[2];
            short[] c1 = new short[2];
            short[] c2 = new short[2];

            var convertor = SelectSampleConvertor(new WaveFormat(48000, 32, 3), AsioSampleType.Int16LSB);
            InvokeConvertor(convertor, input, new Array[] { c0, c1, c2 }, 3, 2);

            Assert.That(c0, Is.EqualTo(new short[] { 1, 3 }));
            Assert.That(c1, Is.EqualTo(new short[] { -1, -3 }));
            Assert.That(c2, Is.EqualTo(new short[] { 2, 4 }));
        }

        [Test]
        public void ConvertorFloatToShortGeneric_ClampsAndConverts()
        {
            float[] input = { -2f, -1f, -0.5f, 0f, 0.5f, 2f };
            short[] c0 = new short[2];
            short[] c1 = new short[2];
            short[] c2 = new short[2];

            var convertor = SelectSampleConvertor(WaveFormat.CreateIeeeFloatWaveFormat(48000, 3), AsioSampleType.Int16LSB);
            InvokeConvertor(convertor, input, new Array[] { c0, c1, c2 }, 3, 2);

            Assert.That(c0, Is.EqualTo(new short[] { -32767, 0 }));
            Assert.That(c1, Is.EqualTo(new short[] { -32767, 16383 }));
            Assert.That(c2, Is.EqualTo(new short[] { -16383, 32767 }));
        }

        [Test]
        public void ConverterFloatTo24LSBGeneric_WritesLittleEndian24Bit()
        {
            float[] input = { 1f, 0f, -1f };
            byte[] c0 = new byte[3];
            byte[] c1 = new byte[3];
            byte[] c2 = new byte[3];

            var convertor = SelectSampleConvertor(WaveFormat.CreateIeeeFloatWaveFormat(48000, 3), AsioSampleType.Int24LSB);
            InvokeConvertor(convertor, input, new Array[] { c0, c1, c2 }, 3, 1);

            Assert.That(c0, Is.EqualTo(new byte[] { 0xFF, 0xFF, 0x7F }));
            Assert.That(c1, Is.EqualTo(new byte[] { 0x00, 0x00, 0x00 }));
            Assert.That(c2, Is.EqualTo(new byte[] { 0x01, 0x00, 0x80 }));
        }

        [Test]
        public void ConverterFloatToFloatGeneric_Deinterleaves()
        {
            float[] input = { 1f, 2f, 3f, 4f, 5f, 6f };
            float[] c0 = new float[2];
            float[] c1 = new float[2];
            float[] c2 = new float[2];

            var convertor = SelectSampleConvertor(WaveFormat.CreateIeeeFloatWaveFormat(48000, 3), AsioSampleType.Float32LSB);
            InvokeConvertor(convertor, input, new Array[] { c0, c1, c2 }, 3, 2);

            Assert.That(c0, Is.EqualTo(new[] { 1f, 4f }));
            Assert.That(c1, Is.EqualTo(new[] { 2f, 5f }));
            Assert.That(c2, Is.EqualTo(new[] { 3f, 6f }));
        }

        [Test]
        public void ConvertorIntToFloatGeneric_NormalizesToMinusOneToOneRange()
        {
            int[] input = { int.MinValue, int.MinValue + 1, 0, int.MaxValue };
            float[] c0 = new float[1];
            float[] c1 = new float[1];
            float[] c2 = new float[1];
            float[] c3 = new float[1];

            var convertor = SelectSampleConvertor(new WaveFormat(48000, 32, 4), AsioSampleType.Float32LSB);
            InvokeConvertor(convertor, input, new Array[] { c0, c1, c2, c3 }, 4, 1);

            Assert.Multiple(() =>
            {
                Assert.That(c0[0], Is.EqualTo(-1f).Within(1e-6));
                Assert.That(c1[0], Is.EqualTo(-1f).Within(1e-6));
                Assert.That(c2[0], Is.EqualTo(0f).Within(1e-6));
                Assert.That(c3[0], Is.EqualTo(0.9999999995f).Within(1e-6));
            });
        }

        private static Delegate SelectSampleConvertor(WaveFormat waveFormat, AsioSampleType asioSampleType)
        {
            var method = ConvertorType.GetMethod("SelectSampleConvertor", BindingFlags.Public | BindingFlags.Static);
            return (Delegate)method.Invoke(null, new object[] { waveFormat, asioSampleType });
        }

        private static void InvokeConvertor(Delegate convertor, Array inputInterleaved, Array[] outputChannels, int channels, int samples)
        {
            var handles = new List<GCHandle>(outputChannels.Length + 1);
            try
            {
                var inputHandle = GCHandle.Alloc(inputInterleaved, GCHandleType.Pinned);
                handles.Add(inputHandle);

                IntPtr[] outputPointers = new IntPtr[channels];
                for (int i = 0; i < channels; i++)
                {
                    var outputHandle = GCHandle.Alloc(outputChannels[i], GCHandleType.Pinned);
                    handles.Add(outputHandle);
                    outputPointers[i] = outputHandle.AddrOfPinnedObject();
                }

                convertor.DynamicInvoke(inputHandle.AddrOfPinnedObject(), outputPointers, channels, samples);
            }
            finally
            {
                foreach (var handle in handles)
                {
                    if (handle.IsAllocated)
                    {
                        handle.Free();
                    }
                }
            }
        }
    }
}
