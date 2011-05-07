using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.IO;
using NAudio.Wave;

namespace NAudioTests.Mp3
{
    [TestFixture]
    public class Mp3FileReaderTests
    {
        [Test]
        public void CanLoadAndReadVariousProblemMp3Files()
        {
            string testDataFolder = @"C:\Users\Mark\Downloads\NAudio";
            foreach (string file in Directory.GetFiles(testDataFolder, "*.mp3"))
            {
                string mp3File = Path.Combine(testDataFolder, file);
                Console.WriteLine("Opening {0}", mp3File);
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
                    Console.WriteLine("Read {0} bytes", total);
                }
            }
        }

    }
}
