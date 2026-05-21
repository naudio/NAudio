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

            var mixer = new MixingWaveProvider32([input1, input2]);

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

            Assert.Throws<ArgumentException>(() => mixer.Read(new byte[10].AsSpan()));
        }

        [Test]
        public void ReadWithNoInputsReturnsZeroAndClearsBuffer()
        {
            var mixer = new MixingWaveProvider32();
            var buffer = new byte[8];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = 0x7F;
            }

            var read = mixer.Read(buffer.AsSpan());

            Assert.That(read, Is.EqualTo(0));
            Assert.That(buffer[0], Is.EqualTo(0));
            Assert.That(buffer[7], Is.EqualTo(0));
        }

        [Test]
        public void ReadSumsAllInputStreams()
        {
            var format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);
            var input1 = new FloatArrayWaveProvider(format, 1f, 2f, 3f, 4f);
            var input2 = new FloatArrayWaveProvider(format, 0.5f, 1.5f, 2.5f, 3.5f);
            var mixer = new MixingWaveProvider32([input1, input2]);

            var buffer = new byte[16];
            var read = mixer.Read(buffer.AsSpan());

            Assert.That(read, Is.EqualTo(16));
            Assert.That(ReadFloats(buffer, 0, read), Is.EqualTo([1.5f, 3.5f, 5.5f, 7.5f]));
        }

        [Test]
        public void ReadReturnsLargestBytesReadFromInputs()
        {
            var format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);
            var shortInput = new FloatArrayWaveProvider(format, 10f, 20f);
            var longInput = new FloatArrayWaveProvider(format, 1f, 2f, 3f, 4f);
            var mixer = new MixingWaveProvider32([shortInput, longInput]);

            var buffer = new byte[16];
            var read = mixer.Read(buffer.AsSpan());

            Assert.That(read, Is.EqualTo(16));
            Assert.That(ReadFloats(buffer, 0, read), Is.EqualTo([11f, 22f, 3f, 4f]));
        }

        [Test]
        public void ReadMixesOnlyBytesReturnedByEachInput()
        {
            var format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);
            var shortInput = new FloatArrayWaveProvider(format, 100f);
            var longInput = new FloatArrayWaveProvider(format, 1f, 2f, 3f);
            var mixer = new MixingWaveProvider32([shortInput, longInput]);

            var buffer = new byte[12];
            var read = mixer.Read(buffer.AsSpan());

            Assert.That(read, Is.EqualTo(12));
            Assert.That(ReadFloats(buffer, 0, read), Is.EqualTo([101f, 2f, 3f]));
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

            public int Read(Span<byte> buffer)
            {
                var available = bytes.Length - position;
                var toCopy = Math.Min(buffer.Length, available);
                if (toCopy > 0)
                {
                    bytes.AsSpan(position, toCopy).CopyTo(buffer);
                    position += toCopy;
                }
                return toCopy;
            }
        }
    }
}
