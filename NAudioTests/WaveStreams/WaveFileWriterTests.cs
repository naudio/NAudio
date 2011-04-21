using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.Wave;
using System.IO;
using NAudio.Utils;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    public class WaveFileWriterTests
    {
        [Test]
        public void ReaderShouldReadBackSameDataWrittenWithWrite()
        {
            MemoryStream ms = new MemoryStream();
            byte[] testSequence = new byte[] { 0x1, 0x2, 0xFF, 0xFE };
            using (WaveFileWriter writer = new WaveFileWriter(new IgnoreDisposeStream(ms), new WaveFormat(16000,24,1)))
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
                for(int n = 0; n < read; n++)
                {
                    Assert.AreEqual(testSequence[n], buffer[n], "Byte " + n.ToString());
                }
            }
        }
    }
}
