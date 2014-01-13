using System;
using System.Linq;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.MediaFoundation
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class MediaFoundationReaderTests
    {
        [Test]
        public void CanReadAnAac()
        {
            var testFile = @"C:\Users\mheath\Downloads\NAudio\AAC\halfspeed.aac";
            var reader = new MediaFoundationReader(testFile);
            Console.WriteLine(reader.WaveFormat);
            var buffer = new byte[reader.WaveFormat.AverageBytesPerSecond];
            int bytesRead;
            long total = 0;
            while((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                Console.WriteLine(bytesRead);
                total += bytesRead;
            }
            Assert.IsTrue(total > 0);
        }
    }
}
