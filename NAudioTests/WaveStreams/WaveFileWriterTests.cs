using System;
using NUnit.Framework;
using NAudio.Wave;
using System.IO;
using NAudio.Utils;
using NAudioTests.Utils;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    [Category("UnitTest")]
    public class WaveFileWriterTests
    {
        [Test]
        public void ReaderShouldReadBackSameDataWrittenWithWrite()
        {
            var ms = new MemoryStream();
            var testSequence = new byte[] { 0x1, 0x2, 0xFF, 0xFE };
            using (var writer = new WaveFileWriter(new IgnoreDisposeStream(ms), new WaveFormat(16000, 24, 1)))
            {
                writer.Write(testSequence, 0, testSequence.Length);
            }
            // check the Reader can read it
            ms.Position = 0;
            using (var reader = new WaveFileReader(ms))
            {
                Assert.AreEqual(16000, reader.WaveFormat.SampleRate, "Sample Rate");
                Assert.AreEqual(24, reader.WaveFormat.BitsPerSample, "Bits Per Sample");
                Assert.AreEqual(1, reader.WaveFormat.Channels, "Channels");
                Assert.AreEqual(testSequence.Length, reader.Length, "File Length");
                var buffer = new byte[600]; // 24 bit audio, block align is 3
                int read = reader.Read(buffer, 0, buffer.Length);
                Assert.AreEqual(testSequence.Length, read, "Data Length");
                for (int n = 0; n < read; n++)
                {
                    Assert.AreEqual(testSequence[n], buffer[n], "Byte " + n);
                }
            }
        }


        [Test]
        public void FlushUpdatesHeaderEvenIfDisposeNotCalled()
        {
            var ms = new MemoryStream();
            var testSequence = new byte[] { 0x1, 0x2, 0xFF, 0xFE };
            var testSequence2 = new byte[] { 0x3, 0x4, 0x5 };
            var writer = new WaveFileWriter(new IgnoreDisposeStream(ms), new WaveFormat(16000, 24, 1));
            writer.Write(testSequence, 0, testSequence.Length);
            writer.Flush();
            // BUT NOT DISPOSED
            // another write that was not flushed
            writer.Write(testSequence2, 0, testSequence2.Length);
            
            // check the Reader can read it
            ms.Position = 0;
            using (var reader = new WaveFileReader(ms))
            {
                Assert.AreEqual(16000, reader.WaveFormat.SampleRate, "Sample Rate");
                Assert.AreEqual(24, reader.WaveFormat.BitsPerSample, "Bits Per Sample");
                Assert.AreEqual(1, reader.WaveFormat.Channels, "Channels");
                Assert.AreEqual(testSequence.Length, reader.Length, "File Length");
                var buffer = new byte[600]; // 24 bit audio, block align is 3
                int read = reader.Read(buffer, 0, buffer.Length);
                Assert.AreEqual(testSequence.Length, read, "Data Length");
                
                for (int n = 0; n < read; n++)
                {
                    Assert.AreEqual(testSequence[n], buffer[n], "Byte " + n);
                }
            }
            writer.Dispose(); // to stop the finalizer from moaning
        }


        [Test]
        public void CreateWaveFileCreatesFileOfCorrectLength()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".wav");
            try
            {
                long length = 4200;
                var waveFormat = new WaveFormat(8000, 8, 2);
                WaveFileWriter.CreateWaveFile(tempFile, new NullWaveStream(waveFormat, length));
                using (var reader = new WaveFileReader(tempFile))
                {
                    Assert.AreEqual(waveFormat, reader.WaveFormat, "WaveFormat");
                    Assert.AreEqual(length, reader.Length, "Length");
                    var buffer = new byte[length + 20];
                    int read = reader.Read(buffer, 0, buffer.Length);
                    Assert.AreEqual(length, read, "Read");
                }
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Test]
        public void CanUseWriteSampleToA16BitFile()
        {
            float amplitude = 0.25f;
            float frequency = 1000;
            using (var writer = new WaveFileWriter(new MemoryStream(), new WaveFormat(16000, 16, 1)))
            {
                for (int n = 0; n < 1000; n++)
                {
                    var sample = (float)(amplitude * Math.Sin((2 * Math.PI * n * frequency) / writer.WaveFormat.SampleRate));
                    writer.WriteSample(sample);
                }
            }
        }

        [Test]
        [Explicit]
        public void CanCreateWaveFileGreaterThan2Gb()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                var dataLength = Int32.MaxValue + 1001L;
                WaveFileWriter.CreateWaveFile(tempFile, new NullWaveStream(new WaveFormat(44100,2), dataLength));
                Assert.AreEqual(dataLength + 46, new FileInfo(tempFile).Length);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Test]
        [Explicit]        
        public void FailsToCreateWaveFileGreaterThan4Gb()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                var dataLength = UInt32.MaxValue - 10; // will be too big as not enough room for RIFF header, fmt chunk etc
                var ae = Assert.Throws<ArgumentException>(
                    () =>
                        WaveFileWriter.CreateWaveFile(tempFile, new NullWaveStream(new WaveFormat(44100, 2), dataLength)));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}
