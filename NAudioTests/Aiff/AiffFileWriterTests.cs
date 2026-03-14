using System;
using System.IO;
using NAudio.Utils;
using NAudio.Wave;
using NAudioTests.Utils;
using NUnit.Framework;

namespace NAudioTests.Aiff
{
    [TestFixture]
    [Category("UnitTest")]
    public class AiffFileWriterTests
    {
        [Test]
        public void ReaderShouldReadBackSameDataWrittenWithWrite()
        {
            var sourceData = new byte[] { 0x10, 0x20, 0x30, 0x40, 0x50, 0x60 };
            var format = new WaveFormat(16000, 24, 1);

            var roundTripped = WriteAndRead(format, writer => writer.Write(sourceData, 0, sourceData.Length));

            Assert.That(roundTripped, Is.EqualTo(sourceData));
        }

        [Test]
        public void WriteWithOffsetAndCountShouldWriteOnlyRequestedSlice()
        {
            var sourceData = new byte[] { 0xAA, 0x11, 0x22, 0x33, 0x44, 0xBB };
            var expectedSlice = new byte[] { 0x11, 0x22, 0x33, 0x44 };
            var format = new WaveFormat(16000, 16, 1);

            var roundTripped = WriteAndRead(format, writer => writer.Write(sourceData, 1, 4));

            Assert.That(roundTripped, Is.EqualTo(expectedSlice));
        }

        [Test]
        public void WriteSample16BitShouldRoundTripExpectedPcmValue()
        {
            var format = new WaveFormat(44100, 16, 1);

            var roundTripped = WriteAndRead(format, writer => writer.WriteSample(1.0f));

            Assert.That(roundTripped, Is.EqualTo(new byte[] { 0xFF, 0x7F }));
        }

        [Test]
        public void WriteSample24BitShouldRoundTripExpectedPcmValue()
        {
            var format = new WaveFormat(44100, 24, 1);

            var roundTripped = WriteAndRead(format, writer =>
            {
                writer.WriteSample(1.0f);
                writer.WriteSample(0.5f);
            });

            Assert.That(roundTripped, Is.EqualTo(new byte[] { 0xFF, 0xFF, 0x7F, 0xFF, 0xFF, 0x3F }));
        }

        [Test]
        public void WriteSample32BitExtensibleShouldNotWriteSilenceForNonZeroSample()
        {
            var format = new WaveFormatExtensible(44100, 32, 1);

            var roundTripped = WriteAndRead(format, writer => writer.WriteSample(0.5f));

            Assert.That(roundTripped, Has.Some.Not.EqualTo((byte)0));
        }

        [Test]
        public void WriteSampleShouldSupportIeeeFloatFormat()
        {
            var format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);

            var roundTripped = WriteAndRead(format, writer => writer.WriteSample(0.5f));

            Assert.That(roundTripped, Is.EqualTo(BitConverter.GetBytes(0.5f)));
        }

        [Test]
        public void WriteSamplesShortTo24BitShouldScaleTo24BitRange()
        {
            var format = new WaveFormat(44100, 24, 1);
            var samples = new short[] { 1, 2 };

            var roundTripped = WriteAndRead(format, writer => writer.WriteSamples(samples, 0, samples.Length));

            Assert.That(roundTripped, Is.EqualTo(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x02, 0x00 }));
        }

        [Test]
        public void WriteSamplesShortTo32BitShouldScaleTo32BitRange()
        {
            var format = new WaveFormatExtensible(44100, 32, 1);
            var samples = new short[] { 1 };

            var roundTripped = WriteAndRead(format, writer => writer.WriteSamples(samples, 0, samples.Length));

            Assert.That(roundTripped, Is.EqualTo(new byte[] { 0x00, 0x00, 0x01, 0x00 }));
        }

        [Test]
        public void CreateAiffFileShouldCreateFileWithExpectedFormatAndLength()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".aiff");
            const int expectedLength = 3000;
            var format = new WaveFormat(22050, 16, 2);

            try
            {
                AiffFileWriter.CreateAiffFile(tempFile, new NullWaveStream(format, expectedLength));

                using (var reader = new AiffFileReader(tempFile))
                {
                    Assert.That(reader.WaveFormat.SampleRate, Is.EqualTo(format.SampleRate));
                    Assert.That(reader.WaveFormat.BitsPerSample, Is.EqualTo(format.BitsPerSample));
                    Assert.That(reader.WaveFormat.Channels, Is.EqualTo(format.Channels));
                    Assert.That(reader.Length, Is.EqualTo(expectedLength));
                }
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        private static byte[] WriteAndRead(WaveFormat format, Action<AiffFileWriter> writeAction)
        {
            var ms = new MemoryStream();

            using (var writer = new AiffFileWriter(new IgnoreDisposeStream(ms), format))
            {
                writeAction(writer);
            }

            ms.Position = 0;
            using (var reader = new AiffFileReader(ms))
            {
                var buffer = new byte[(int)reader.Length];
                var read = reader.Read(buffer, 0, buffer.Length);
                Assert.That(read, Is.EqualTo(buffer.Length));
                return buffer;
            }
        }
    }
}
