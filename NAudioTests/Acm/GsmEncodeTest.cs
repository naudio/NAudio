using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NAudio.Wave;
using System.IO;

namespace NAudioTests.Acm
{
    [TestFixture]
    public class GsmEncodeTest
    {
        [Test]
        public void CanEncodeGsm()
        {
            var testFile = @"C:\Users\Mark\Code\CodePlex\AudioTestFiles\WAV\PCM 16 bit\pcm mono 16 bit 8kHz.wav";
            if (!File.Exists(testFile))
            {
                Assert.Ignore("Missing test file");
            }
            using (var reader = new WaveFileReader(testFile))
            {
                using (var gsm = new WaveFormatConversionStream(new Gsm610WaveFormat(), reader))
                {
                    WaveFileWriter.CreateWaveFile(@"C:\Users\Mark\Code\CodePlex\gsm.wav", gsm);
                }
            }
        }

        [Test]
        public void CanDecodeGsm()
        {
            var testFile = @"C:\Users\Mark\Code\CodePlex\gsm.wav";
            if (!File.Exists(testFile))
            {
                Assert.Ignore("Missing test file");
            }
            using (var reader = new WaveFileReader(testFile))
            {
                using (var pcm = WaveFormatConversionStream.CreatePcmStream(reader))
                {
                    WaveFileWriter.CreateWaveFile(@"C:\Users\Mark\Code\CodePlex\gsm-decoded.wav", pcm);
                }
            }
        }
    }
}
