using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.Wave;

namespace NAudioTests
{
    [TestFixture]
    public class ResamplerDmoStreamTests
    {
        [Test]
        public void CanCreateResamplerStream()
        {
            using (WaveFileReader reader = new WaveFileReader("C:\\Users\\Mark\\Recording\\REAPER\\ideas-2008-05-17.wav"))
            {
                using (ResamplerDmoStream resampler = new ResamplerDmoStream(reader, WaveFormat.CreateIeeeFloatWaveFormat(48000,2)))
                {
                    Assert.Greater(resampler.Length, reader.Length, "Length");
                    Assert.AreEqual(0, reader.Position, "Position");
                    Assert.AreEqual(0, resampler.Position, "Position");            
                }
            }
        }

        [Test]
        public void CanReadABlockFromResamplerStream()
        {
            using (WaveFileReader reader = new WaveFileReader("C:\\Users\\Mark\\Recording\\REAPER\\ideas-2008-05-17.wav"))
            {
                using (ResamplerDmoStream resampler = new ResamplerDmoStream(reader, WaveFormat.CreateIeeeFloatWaveFormat(48000, 2)))
                {
                    // try to read 10 ms;
                    int bytesToRead = resampler.WaveFormat.AverageBytesPerSecond / 100;
                    byte[] buffer = new byte[bytesToRead];
                    int count = resampler.Read(buffer, 0, bytesToRead);
                    Assert.AreEqual(count, bytesToRead, "Bytes Read");
                }
            }
        }
    }
}
