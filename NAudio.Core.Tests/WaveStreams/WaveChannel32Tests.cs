using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.Wave;
using NAudio.Core.Tests.Utils;
using System.IO;

namespace NAudio.Core.Tests.WaveStreams
{
    [TestFixture]
    public class WaveChannel32Tests
    {
        [Test]
        [Category("IntegrationTest")]
        public void CanCreateWavFileFromWaveChannel32()
        {
            string inFile = @"F:\Recording\wav\pcm\16bit mono 8kHz.wav";
            string outFile = @"F:\Recording\wav\pcm\32bit stereo 8kHz.wav";
            if (!File.Exists(inFile))
            {
                Assert.Ignore("Input test file not found");
            }
            var audio32 = new WaveChannel32(new WaveFileReader(inFile));
            audio32.PadWithZeroes = false;
            WaveFileWriter.CreateWaveFile(outFile, audio32);
        }
    }
}
