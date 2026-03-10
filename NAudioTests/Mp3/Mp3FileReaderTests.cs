using System;
using NUnit.Framework;
using System.IO;
using NAudio.Wave;
using System.Diagnostics;
using NAudio.MediaFoundation;
using NAudio.Wave.SampleProviders;
using NAudioTests.Utils;

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
                Assert.Ignore($"{testDataFolder} not found");
            }
            foreach (string file in Directory.GetFiles(testDataFolder, "*.mp3"))
            {
                string mp3File = Path.Combine(testDataFolder, file);
                Debug.WriteLine($"Opening {mp3File}");
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
                    Debug.WriteLine($"Read {total} bytes");
                }
            }
        }

        [Test]
        public void ReadFrameAdvancesPosition()
        {
            var file = TestFileBuilder.CreateMp3File(5);
            try
            {
                using (var mp3FileReader = new Mp3FileReader(file))
                {
                    var lastPos = mp3FileReader.Position;
                    while ((mp3FileReader.ReadNextFrame()) != null)
                    {
                        Assert.That(mp3FileReader.Position, Is.GreaterThan(lastPos));
                        lastPos = mp3FileReader.Position;
                    }
                    Assert.That(mp3FileReader.Position, Is.EqualTo(mp3FileReader.Length));
                    Assert.That(mp3FileReader.Length, Is.GreaterThan(0));
                }
            }
            finally
            {
                File.Delete(file);
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
