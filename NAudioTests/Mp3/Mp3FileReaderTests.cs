using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.IO;
using NAudio.Wave;
using System.Diagnostics;

namespace NAudioTests.Mp3
{
    [TestFixture]
    public class Mp3FileReaderTests
    {
        [Test]
        [Category("IntegrationTest")]
        public void CanLoadAndReadVariousProblemMp3Files()
        {
            string testDataFolder = @"C:\Users\Mark\Downloads\NAudio";
            if (!Directory.Exists(testDataFolder))
            {
                Assert.Ignore("{0} not found", testDataFolder);
            }
            foreach (string file in Directory.GetFiles(testDataFolder, "*.mp3"))
            {
                string mp3File = Path.Combine(testDataFolder, file);
                Debug.WriteLine(String.Format("Opening {0}", mp3File));
                using (var reader = new Mp3FileReader(mp3File))
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    int total = 0;
                    do
                    {
                        bytesRead = reader.Read(buffer, 0, buffer.Length);
                        total += bytesRead;
                    } while (bytesRead > 0);
                    Debug.WriteLine(String.Format("Read {0} bytes", total));
                }
            }
        }

        [Test]
        public void CopesWithZeroLengthMp3()
        {
            var ms = new MemoryStream(new byte[0]);
            Assert.Throws<InvalidDataException>(() => new Mp3FileReader(ms));            
        }
    }
}
