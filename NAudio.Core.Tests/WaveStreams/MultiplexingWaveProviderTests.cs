using System;
using NUnit.Framework;
using NAudio.Wave;
using System.Diagnostics;
using Moq;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    public class MultiplexingWaveProviderTests
    {
        [Test]
        public void NullInputsShouldThrowException()
        {
            Assert.Throws<ArgumentNullException>(() => new MultiplexingWaveProvider(null, 1));
        }

        [Test]
        public void ZeroInputsShouldThrowException()
        {
            Assert.Throws<ArgumentException>(() => new MultiplexingWaveProvider([], 1));
        }

        [Test]
        public void ZeroOutputsShouldThrowException()
        {
            var input1 = new Mock<IWaveProvider>();
            Assert.Throws<ArgumentException>(() => new MultiplexingWaveProvider([input1.Object], 0));
        }

        [Test]
        public void InvalidWaveFormatShouldThowException()
        {
            var input1 = new Mock<IWaveProvider>();
            input1.Setup(x => x.WaveFormat).Returns(new Gsm610WaveFormat());
            Assert.Throws<ArgumentException>(() => new MultiplexingWaveProvider([input1.Object], 1));
        }

        [Test]
        public void OneInOneOutShouldCopyWaveFormat()
        {
            var input1 = new Mock<IWaveProvider>();
            var inputWaveFormat = new WaveFormat(32000, 16, 1);
            input1.Setup(x => x.WaveFormat).Returns(inputWaveFormat);
            var mp = new MultiplexingWaveProvider([input1.Object], 1);
            Assert.That(mp.WaveFormat, Is.EqualTo(inputWaveFormat));
        }

        [Test]
        public void OneInTwoOutShouldCopyWaveFormatButBeStereo()
        {
            var input1 = new Mock<IWaveProvider>();
            var inputWaveFormat = new WaveFormat(32000, 16, 1);
            input1.Setup(x => x.WaveFormat).Returns(inputWaveFormat);
            var mp = new MultiplexingWaveProvider([input1.Object], 2);
            var expectedOutputWaveFormat = new WaveFormat(32000, 16, 2);
            Assert.That(mp.WaveFormat, Is.EqualTo(expectedOutputWaveFormat));
        }

        [Test]
        public void OneInOneOutShouldCopyInReadMethod()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 1));
            byte[] expected = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
            var mp = new MultiplexingWaveProvider([input1], 1);
            byte[] buffer = new byte[10];
            var read = mp.Read(buffer.AsSpan());
            Assert.That(read, Is.EqualTo(10));
            Assert.That(buffer, Is.EqualTo(expected));
        }

        [Test]
        public void OneInTwoOutShouldConvertMonoToStereo()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 1));
            // 16 bit so left right pairs
            byte[] expected = [0, 1, 0, 1, 2, 3, 2, 3, 4, 5, 4, 5, 6, 7, 6, 7, 8, 9, 8, 9];
            var mp = new MultiplexingWaveProvider([input1], 2);
            byte[] buffer = new byte[20];
            var read = mp.Read(buffer.AsSpan());
            Assert.That(read, Is.EqualTo(20));
            Assert.That(buffer, Is.EqualTo(expected));
        }

        [Test]
        public void TwoInOneOutShouldSelectLeftChannel()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 2));
            // 16 bit so left right pairs
            byte[] expected = [0, 1, 4, 5, 8, 9, 12, 13, 16, 17];
            var mp = new MultiplexingWaveProvider([input1], 1);
            byte[] buffer = new byte[10];
            var read = mp.Read(buffer.AsSpan());
            Assert.That(read, Is.EqualTo(10));
            Assert.That(buffer, Is.EqualTo(expected));
        }

        [Test]
        public void TwoInOneOutShouldCanBeConfiguredToSelectRightChannel()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 2));
            // 16 bit so left right pairs
            byte[] expected = [2, 3, 6, 7, 10, 11, 14, 15, 18, 19];
            var mp = new MultiplexingWaveProvider([input1], 1);
            mp.ConnectInputToOutput(1, 0);
            byte[] buffer = new byte[10];
            var read = mp.Read(buffer.AsSpan());
            Assert.That(read, Is.EqualTo(10));
            Assert.That(buffer, Is.EqualTo(expected));
        }

        [Test]
        public void StereoInTwoOutShouldCopyStereo()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 2));
            // 4 bytes per pair of samples
            byte[] expected = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];
            var mp = new MultiplexingWaveProvider([input1], 2);
            byte[] buffer = new byte[12];
            var read = mp.Read(buffer.AsSpan());
            Assert.That(read, Is.EqualTo(12));
            Assert.That(buffer, Is.EqualTo(expected));
        }

        [Test]
        public void TwoMonoInTwoOutShouldCreateStereo()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 1));
            var input2 = new TestWaveProvider(new WaveFormat(32000, 16, 1)) { Position = 100 };
            // 4 bytes per pair of samples
            byte[] expected = [0, 1, 100, 101, 2, 3, 102, 103, 4, 5, 104, 105,];
            var mp = new MultiplexingWaveProvider([input1, input2], 2);
            byte[] buffer = new byte[expected.Length];
            var read = mp.Read(buffer.AsSpan());
            Assert.That(read, Is.EqualTo(expected.Length));
            Assert.That(buffer, Is.EqualTo(expected));
        }

        [Test]
        public void StereoInTwoOutCanBeConfiguredToSwapLeftAndRight()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 2));
            // 4 bytes per pair of samples
            byte[] expected = [2, 3, 0, 1, 6, 7, 4, 5, 10, 11, 8, 9,];
            var mp = new MultiplexingWaveProvider([input1], 2);
            mp.ConnectInputToOutput(0, 1);
            mp.ConnectInputToOutput(1, 0);
            byte[] buffer = new byte[12];
            var read = mp.Read(buffer.AsSpan());
            Assert.That(read, Is.EqualTo(12));
            Assert.That(buffer, Is.EqualTo(expected));
        }

        [Test]
        public void HasConnectInputToOutputMethod()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 2));
            var mp = new MultiplexingWaveProvider([input1], 1);
            mp.ConnectInputToOutput(1, 0);
        }

        [Test]
        public void ConnectInputToOutputThrowsExceptionForInvalidInput()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 2));
            var mp = new MultiplexingWaveProvider([input1], 1);
            Assert.Throws<ArgumentException>(() => mp.ConnectInputToOutput(2, 0));
        }

        [Test]
        public void ConnectInputToOutputThrowsExceptionForInvalidOutput()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 2));
            var mp = new MultiplexingWaveProvider([input1], 1);
            Assert.Throws<ArgumentException>(() => mp.ConnectInputToOutput(1, 1));
        }

        [Test]
        public void InputChannelCountIsCorrect()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 2));
            var input2 = new TestWaveProvider(new WaveFormat(32000, 16, 1));
            var mp = new MultiplexingWaveProvider([input1, input2], 1);
            Assert.That(mp.InputChannelCount, Is.EqualTo(3));
        }

        [Test]
        public void OutputChannelCountIsCorrect()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 1));
            var mp = new MultiplexingWaveProvider([input1], 3);
            Assert.That(mp.OutputChannelCount, Is.EqualTo(3));
        }

        [Test]
        public void ThrowsExceptionIfSampleRatesDiffer()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 2));
            var input2 = new TestWaveProvider(new WaveFormat(44100, 16, 1));
            Assert.Throws<ArgumentException>(() => new MultiplexingWaveProvider([input1, input2], 1));
        }

        [Test]
        public void ThrowsExceptionIfBitDepthsDiffer()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 2));
            var input2 = new TestWaveProvider(new WaveFormat(32000, 24, 1));
            Assert.Throws<ArgumentException>(() => new MultiplexingWaveProvider([input1, input2], 1));
        }

        [Test]
        public void ReadReturnsZeroIfSingleInputHasReachedEnd()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 1), 0);
            byte[] expected = [];
            var mp = new MultiplexingWaveProvider([input1], 1);
            byte[] buffer = new byte[10];
            var read = mp.Read(buffer.AsSpan());
            Assert.That(read, Is.EqualTo(0));
        }

        [Test]
        public void ReadReturnsCountIfOneInputHasEndedButTheOtherHasnt()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 1), 0);
            var input2 = new TestWaveProvider(new WaveFormat(32000, 16, 1));
            byte[] expected = [];
            var mp = new MultiplexingWaveProvider([input1, input2], 1);
            byte[] buffer = new byte[10];
            var read = mp.Read(buffer.AsSpan());
            Assert.That(read, Is.EqualTo(10));
        }

        [Test]
        public void SingleInputConstructorUsesTotalOfInputChannels()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 8, 2));
            var input2 = new TestWaveProvider(new WaveFormat(32000, 8, 1));
            byte[] expected = [0,1,0,2,3,1,4,5,2];
            var mp = new MultiplexingWaveProvider([input1, input2]);
            Assert.That(mp.WaveFormat.Channels, Is.EqualTo(3));
            byte[] buffer = new byte[9];
            var read = mp.Read(buffer.AsSpan());
            Assert.That(read, Is.EqualTo(9));
            Assert.That(buffer, Is.EqualTo(expected));
        }

        [Test]
        public void ShouldZeroOutBufferIfInputStopsShort()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 16, 1), 6);
            byte[] expected = [0, 1, 2, 3, 4, 5, 0, 0, 0, 0];
            var mp = new MultiplexingWaveProvider([input1], 1);
            byte[] buffer = new byte[10];
            for (int n = 0; n < buffer.Length; n++)
            {
                buffer[n] = 0xFF;
            }
            var read = mp.Read(buffer.AsSpan());
            Assert.That(read, Is.EqualTo(6));
            Assert.That(buffer, Is.EqualTo(expected));
        }

        [Test]
        public void CorrectlyHandles24BitAudio()
        {
            var input1 = new TestWaveProvider(new WaveFormat(32000, 24, 1));
            byte[] expected = [0, 1, 2, 0, 1, 2, 3, 4, 5, 3, 4, 5, 6, 7, 8, 6, 7, 8, 9, 10, 11, 9, 10, 11];
            var mp = new MultiplexingWaveProvider([input1], 2);
            byte[] buffer = new byte[expected.Length];
            var read = mp.Read(buffer.AsSpan());
            Assert.That(read, Is.EqualTo(expected.Length));
            Assert.That(buffer, Is.EqualTo(expected));
        }

        [Test]
        public void CorrectlyHandlesIeeeFloat()
        {
            var input1 = new TestWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(32000, 1));
            byte[] expected = [0, 1, 2, 3, 0, 1, 2, 3, 4, 5, 6, 7, 4, 5, 6, 7, 8, 9, 10, 11, 8, 9, 10, 11,];
            var mp = new MultiplexingWaveProvider([input1], 2);
            byte[] buffer = new byte[expected.Length];
            var read = mp.Read(buffer.AsSpan());
            Assert.That(read, Is.EqualTo(expected.Length));
            Assert.That(buffer, Is.EqualTo(expected));
        }

        [Test]
        public void CorrectOutputFormatIsSetForIeeeFloat()
        {
            var input1 = new TestWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(32000, 1));
            byte[] expected = [0, 1, 2, 3, 0, 1, 2, 3, 4, 5, 6, 7, 4, 5, 6, 7, 8, 9, 10, 11, 8, 9, 10, 11,];
            var mp = new MultiplexingWaveProvider([input1], 2);
            Assert.That(mp.WaveFormat.Encoding, Is.EqualTo(WaveFormatEncoding.IeeeFloat));
        }

        public void PerformanceTest()
        {
            var waveFormat = new WaveFormat(32000, 16, 1);
            var input1 = new TestWaveProvider(waveFormat);
            var input2 = new TestWaveProvider(waveFormat);
            var input3 = new TestWaveProvider(waveFormat);
            var input4 = new TestWaveProvider(waveFormat);
            var mp = new MultiplexingWaveProvider([input1, input2, input3, input4], 4);
            mp.ConnectInputToOutput(0, 3);
            mp.ConnectInputToOutput(1, 2);
            mp.ConnectInputToOutput(2, 1);
            mp.ConnectInputToOutput(3, 0);

            byte[] buffer = new byte[waveFormat.AverageBytesPerSecond];
            Stopwatch s = new Stopwatch();
            var duration = s.Time(() =>
            {
                // read one hour worth of audio
                for (int n = 0; n < 60 * 60; n++)
                {
                    mp.Read(buffer.AsSpan());
                }
            });
            Console.WriteLine("Performance test took {0}ms", duration);
        }
    }
}
