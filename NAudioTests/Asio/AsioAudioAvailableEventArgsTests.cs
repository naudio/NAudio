using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NAudio.Wave;
using NAudio.Wave.Asio;
using NUnit.Framework;

namespace NAudioTests.Asio
{
    [TestFixture]
    [Category("UnitTest")]
    public class AsioAudioAvailableEventArgsTests
    {
        [Test]
        public void GetAsInterleavedSamples_Int32LSB_InterleavesAndConverts()
        {
            var inputChannels = new Array[]
            {
                new[] { int.MinValue, int.MaxValue },
                new[] { 0, 1073741824 }
            };

            var samples = new float[4];
            var written = GetInterleavedSamples(AsioSampleType.Int32LSB, inputChannels, 2, samples);

            Assert.That(written, Is.EqualTo(4));
            Assert.Multiple(() =>
            {
                Assert.That(samples[0], Is.EqualTo(int.MinValue / (float)int.MaxValue).Within(1e-6f));
                Assert.That(samples[1], Is.EqualTo(0f).Within(1e-6f));
                Assert.That(samples[2], Is.EqualTo(1f).Within(1e-6f));
                Assert.That(samples[3], Is.EqualTo(0.5f).Within(1e-6f));
            });
        }

        [Test]
        public void GetAsInterleavedSamples_Int16LSB_InterleavesAndConverts()
        {
            var inputChannels = new Array[]
            {
                new short[] { short.MinValue, short.MaxValue },
                new short[] { 0, 16384 }
            };

            var samples = new float[4];
            var written = GetInterleavedSamples(AsioSampleType.Int16LSB, inputChannels, 2, samples);

            Assert.That(written, Is.EqualTo(4));
            Assert.Multiple(() =>
            {
                Assert.That(samples[0], Is.EqualTo(short.MinValue / (float)short.MaxValue).Within(1e-6f));
                Assert.That(samples[1], Is.EqualTo(0f).Within(1e-6f));
                Assert.That(samples[2], Is.EqualTo(1f).Within(1e-6f));
                Assert.That(samples[3], Is.EqualTo(16384f / short.MaxValue).Within(1e-6f));
            });
        }

        [Test]
        public void GetAsInterleavedSamples_Int24LSB_InterleavesAndConverts()
        {
            var inputChannels = new Array[]
            {
                new byte[] { 0x01, 0x00, 0x00, 0xFF, 0xFF, 0x7F },
                new byte[] { 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x80 }
            };

            var samples = new float[4];
            var written = GetInterleavedSamples(AsioSampleType.Int24LSB, inputChannels, 2, samples);

            Assert.That(written, Is.EqualTo(4));
            Assert.Multiple(() =>
            {
                Assert.That(samples[0], Is.EqualTo(1f / 8388608f).Within(1e-7f));
                Assert.That(samples[1], Is.EqualTo(-1f / 8388608f).Within(1e-7f));
                Assert.That(samples[2], Is.EqualTo(8388607f / 8388608f).Within(1e-6f));
                Assert.That(samples[3], Is.EqualTo(-1f).Within(1e-7f));
            });
        }

        [Test]
        public void GetAsInterleavedSamples_Float32LSB_InterleavesWithoutConversion()
        {
            var inputChannels = new Array[]
            {
                new[] { -1f, 0.25f },
                new[] { 0.5f, 1f }
            };

            var samples = new float[4];
            var written = GetInterleavedSamples(AsioSampleType.Float32LSB, inputChannels, 2, samples);

            Assert.That(written, Is.EqualTo(4));
            Assert.That(samples, Is.EqualTo(new[] { -1f, 0.5f, 0.25f, 1f }));
        }

        [Test]
        public void GetAsInterleavedSamples_ThrowsForUnsupportedSampleType()
        {
            var inputChannels = new Array[] { new[] { 0 }, new[] { 0 } };
            var samples = new float[2];

            Assert.Throws<NotImplementedException>(() => GetInterleavedSamples(AsioSampleType.Int32MSB, inputChannels, 1, samples));
        }

        [Test]
        public void GetAsInterleavedSamples_ThrowsWhenOutputBufferTooSmall()
        {
            var inputChannels = new Array[] { new[] { 0 }, new[] { 0 } };
            var samples = new float[1];

            Assert.Throws<ArgumentException>(() => GetInterleavedSamples(AsioSampleType.Int32LSB, inputChannels, 1, samples));
        }

        private static int GetInterleavedSamples(AsioSampleType sampleType, Array[] inputChannels, int samplesPerBuffer, float[] destination)
        {
            var handles = new List<GCHandle>(inputChannels.Length);
            try
            {
                var inputPointers = new IntPtr[inputChannels.Length];
                for (int i = 0; i < inputChannels.Length; i++)
                {
                    var handle = GCHandle.Alloc(inputChannels[i], GCHandleType.Pinned);
                    handles.Add(handle);
                    inputPointers[i] = handle.AddrOfPinnedObject();
                }

                var args = new AsioAudioAvailableEventArgs(inputPointers, Array.Empty<IntPtr>(), samplesPerBuffer, sampleType);
                return args.GetAsInterleavedSamples(destination);
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
