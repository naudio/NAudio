using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.Wave;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    [Category("UnitTest")]
    public class MonoToStereoProvider16Tests
    {
        [Test]
        public void LeftChannelOnly()
        {
            IWaveProvider monoStream = new TestMonoProvider();
            MonoToStereoProvider16 stereo = new MonoToStereoProvider16(monoStream);
            stereo.LeftVolume = 1.0f;
            stereo.RightVolume = 0.0f;
            int samples = 1000;
            byte[] buffer = new byte[samples * 2];
            int read = stereo.Read(buffer, 0, buffer.Length);
            Assert.AreEqual(buffer.Length, read, "bytes read");
            WaveBuffer waveBuffer = new WaveBuffer(buffer);
            short expected = 0;
            for (int sample = 0; sample < samples; sample+=2)
            {
                short sampleLeft = waveBuffer.ShortBuffer[sample];
                short sampleRight = waveBuffer.ShortBuffer[sample+1];
                Assert.AreEqual(expected++, sampleLeft, "sample left");
                Assert.AreEqual(0, sampleRight, "sample right");
            }
        }
    }

    class TestMonoProvider : WaveProvider16
    {
        short current;

        public override int Read(short[] buffer, int offset, int sampleCount)
        {
            for (int sample = 0; sample < sampleCount; sample++)
            {
                buffer[offset + sample] = current++;
            }
            return sampleCount;
        }
    }
}
