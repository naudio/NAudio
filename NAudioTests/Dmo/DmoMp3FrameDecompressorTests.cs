using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.FileFormats.Mp3;
using NAudio.Wave;

namespace NAudioTests.Dmo
{
    [TestFixture]
    public class DmoMp3FrameDecompressorTests
    {
        [Test]
        public void CanCreateDmoMp3FrameDecompressor()
        {
            Mp3WaveFormat mp3Format = new Mp3WaveFormat(44100,2,215,32000);
            DmoMp3FrameDecompressor frameDecompressor = new DmoMp3FrameDecompressor(mp3Format);
        }
    }
}
