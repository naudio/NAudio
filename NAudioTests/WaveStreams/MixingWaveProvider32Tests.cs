using System;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    public class MixingWaveProvider32Tests
    {
        [Test]
        public void DefaultConstructorUsesStereo44100FloatFormat()
        {
            var mixer = new MixingWaveProvider32();

            Assert.That(mixer.InputCount, Is.EqualTo(0));
            Assert.That(mixer.WaveFormat.Encoding, Is.EqualTo(WaveFormatEncoding.IeeeFloat));
            Assert.That(mixer.WaveFormat.BitsPerSample, Is.EqualTo(32));
            Assert.That(mixer.WaveFormat.SampleRate, Is.EqualTo(44100));
            Assert.That(mixer.WaveFormat.Channels, Is.EqualTo(2));
        }

        [Test]
        public void ConstructorWithInputsSetsWaveFormatFromFirstInput()
        {
            var format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);
            var input1 = new FloatArrayWaveProvider(format, 1f, 2f);
            var input2 = new FloatArrayWaveProvider(format, 3f, 4f);

            var mixer = new MixingWaveProvider32(new IWaveProvider[] { input1, input2 });

            Assert.That(mixer.InputCount, Is.EqualTo(2));
            Assert.That(mixer.WaveFormat, Is.EqualTo(format));
        }

        [Test]
        public void AddInputStreamRejectsNonFloatFormats()
        {
            var mixer = new MixingWaveProvider32();
            var pcmInput = new TestWaveProvider(new WaveFormat(44100, 16, 2));

            Assert.Throws<ArgumentException>(() => mixer.AddInputStream(pcmInput));
        }

        [Test]
        public void AddInputStreamRejectsDifferentFormats()
        {
            var mixer = new MixingWaveProvider32();
            var first = new FloatArrayWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2), 1f);
            var second = new FloatArrayWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2), 1f);

            mixer.AddInputStream(first);

            Assert.Throws<ArgumentException>(() => mixer.AddInputStream(second));
        }

        [Test]
        public void RemoveInputStreamRemovesInput()
        {
            var mixer = new MixingWaveProvider32();
            var input = new FloatArrayWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2), 1f);

            mixer.AddInputStream(input);
            mixer.RemoveInputStream(input);

            Assert.That(mixer.InputCount, Is.EqualTo(0));
        }

        [Test]
        public void ReadThrowsWhenCountIsNotAWholeNumberOfSamples()
        {
            var mixer = new MixingWaveProvider32();

            Assert.Throws<ArgumentException>(() => mixer.Read(new byte[10], 0, 10));
        }

        [Test]
        public void ReadWithNoInputsReturnsZeroAndClearsRequestedRegion()
        {
            var mixer = new MixingWaveProvider32();
            var buffer = new byte[16];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = 0x7F;
            }

            var read = mixer.Read(buffer, 4, 8);

            Assert.That(read, Is.EqualTo(0));
            Assert.That(buffer[0], Is.EqualTo(0x7F));
            Assert.That(buffer[1], Is.EqualTo(0x7F));
            Assert.That(buffer[2], Is.EqualTo(0x7F));
            Assert.That(buffer[3], Is.EqualTo(0x7F));
            Assert.That(buffer[4], Is.EqualTo(0));
            Assert.That(buffer[11], Is.EqualTo(0));
            Assert.That(buffer[12], Is.EqualTo(0x7F));
            Assert.That(buffer[15], Is.EqualTo(0x7F));
        }

        [Test]
        public void ReadSumsAllInputStreams()
        {
            var format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);
            var input1 = new FloatArrayWaveProvider(format, 1f, 2f, 3f, 4f);
            var input2 = new FloatArrayWaveProvider(format, 0.5f, 1.5f, 2.5f, 3.5f);
            var mixer = new MixingWaveProvider32(new IWaveProvider[] { input1, input2 });

            var buffer = new byte[16];
            var read = mixer.Read(buffer, 0, buffer.Length);

            Assert.That(read, Is.EqualTo(16));
            Assert.That(ReadFloats(buffer, 0, read), Is.EqualTo(new[] { 1.5f, 3.5f, 5.5f, 7.5f }));
        }

        [Test]
        public void ReadReturnsLargestBytesReadFromInputs()
        {
            var format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);
            var shortInput = new FloatArrayWaveProvider(format, 10f, 20f);
            var longInput = new FloatArrayWaveProvider(format, 1f, 2f, 3f, 4f);
            var mixer = new MixingWaveProvider32(new IWaveProvider[] { shortInput, longInput });

            var buffer = new byte[16];
            var read = mixer.Read(buffer, 0, buffer.Length);

            Assert.That(read, Is.EqualTo(16));
            Assert.That(ReadFloats(buffer, 0, read), Is.EqualTo(new[] { 11f, 22f, 3f, 4f }));
        }

        [Test]
        public void ReadUsesOffsetWhenWritingMixedAudio()
        {
            var format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);
            var input = new FloatArrayWaveProvider(format, 10f, 20f);
            var mixer = new MixingWaveProvider32(new IWaveProvider[] { input });

            var buffer = FloatsToBytes(999f, 999f, 999f, 999f);
            var read = mixer.Read(buffer, 4, 8);

            Assert.That(read, Is.EqualTo(8));
            Assert.That(ReadFloats(buffer, 0, buffer.Length), Is.EqualTo(new[] { 999f, 10f, 20f, 999f }));
        }

        [Test]
        public void ReadMixesOnlyBytesReturnedByEachInput()
        {
            var format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);
            var shortInput = new FloatArrayWaveProvider(format, 100f);
            var longInput = new FloatArrayWaveProvider(format, 1f, 2f, 3f);
            var mixer = new MixingWaveProvider32(new IWaveProvider[] { shortInput, longInput });

            var buffer = new byte[12];
            var read = mixer.Read(buffer, 0, buffer.Length);

            Assert.That(read, Is.EqualTo(12));
            Assert.That(ReadFloats(buffer, 0, read), Is.EqualTo(new[] { 101f, 2f, 3f }));
        }

        [Test]
        public void ConstructorThrowsForNullInputs()
        {
            Assert.Throws<ArgumentNullException>(() => new MixingWaveProvider32(null));
        }

        [Test]
        public void AddInputStreamThrowsForNullInput()
        {
            var mixer = new MixingWaveProvider32();

            Assert.Throws<ArgumentNullException>(() => mixer.AddInputStream(null));
        }

        [Test]
        public void ReadThrowsWhenOffsetIsNotOnSampleBoundary()
        {
            var mixer = new MixingWaveProvider32();

            Assert.Throws<ArgumentException>(() => mixer.Read(new byte[12], 2, 8));
        }

        private static float[] ReadFloats(byte[] buffer, int offset, int byteCount)
        {
            var data = new byte[byteCount];
            Buffer.BlockCopy(buffer, offset, data, 0, byteCount);
            var result = new float[byteCount / 4];
            Buffer.BlockCopy(data, 0, result, 0, byteCount);
            return result;
        }

        private static byte[] FloatsToBytes(params float[] floats)
        {
            var bytes = new byte[floats.Length * 4];
            Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private sealed class FloatArrayWaveProvider : IWaveProvider
        {
            private readonly byte[] bytes;
            private int position;

            public FloatArrayWaveProvider(WaveFormat waveFormat, params float[] samples)
            {
                WaveFormat = waveFormat;
                bytes = FloatsToBytes(samples);
            }

            public WaveFormat WaveFormat { get; }

            public int Read(byte[] buffer, int offset, int count)
            {
                var available = bytes.Length - position;
                var toCopy = Math.Min(count, available);
                if (toCopy > 0)
                {
                    Buffer.BlockCopy(bytes, position, buffer, offset, toCopy);
                    position += toCopy;
                }
                return toCopy;
            }
        }
    }
}
