using System;
using System.Collections.Generic;
using System.Text;
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
            MemoryStream ms = new MemoryStream();
            byte[] testSequence = new byte[] { 0x1, 0x2, 0xFF, 0xFE };
            using (WaveFileWriter writer = new WaveFileWriter(new IgnoreDisposeStream(ms), new WaveFormat(16000, 24, 1)))
            {
                writer.Write(testSequence, 0, testSequence.Length);
            }
            // check the Reader can read it
            ms.Position = 0;
            using (WaveFileReader reader = new WaveFileReader(ms))
            {
                Assert.AreEqual(16000, reader.WaveFormat.SampleRate, "Sample Rate");
                Assert.AreEqual(24, reader.WaveFormat.BitsPerSample, "Bits Per Sample");
                Assert.AreEqual(1, reader.WaveFormat.Channels, "Channels");
                Assert.AreEqual(testSequence.Length, reader.Length, "File Length");
                byte[] buffer = new byte[600]; // 24 bit audio, block align is 3
                int read = reader.Read(buffer, 0, buffer.Length);
                Assert.AreEqual(testSequence.Length, read, "Data Length");
                for (int n = 0; n < read; n++)
                {
                    Assert.AreEqual(testSequence[n], buffer[n], "Byte " + n.ToString());
                }
            }
        }

        [Test]
        public void CreateWaveFileCreatesFileOfCorrectLength()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".wav");
            try
            {
                long length = 4200;
                WaveFormat waveFormat = new WaveFormat(8000, 8, 2);
                WaveFileWriter.CreateWaveFile(tempFile, new NullWaveStream(waveFormat, length));
                using (WaveFileReader reader = new WaveFileReader(tempFile))
                {
                    Assert.AreEqual(waveFormat, reader.WaveFormat, "WaveFormat");
                    Assert.AreEqual(length, reader.Length, "Length");
                    byte[] buffer = new byte[length + 20];
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
            using (WaveFileWriter writer = new WaveFileWriter(new MemoryStream(), new WaveFormat(16000, 16, 1)))
            {
                for (int n = 0; n < 1000; n++)
                {
                    float sample = (float)(amplitude * Math.Sin((2 * Math.PI * n * frequency) / writer.WaveFormat.SampleRate));
                    writer.WriteSample(sample);
                }
            }
        }
    }
}
