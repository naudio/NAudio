using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.Wave;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    [Category("UnitTest")]
    public class StereoToMonoProvider16Tests
    {
        [Test]
        public void RightChannelOnly()
        {
            IWaveProvider stereoStream = new TestStereoProvider();
            StereoToMonoProvider16 mono = new StereoToMonoProvider16(stereoStream);
            mono.LeftVolume = 0.0f;
            mono.RightVolume = 1.0f;
            int samples = 1000;
            byte[] buffer = new byte[samples * 2];
            int read = mono.Read(buffer, 0, buffer.Length);
            Assert.AreEqual(buffer.Length, read, "bytes read");
            WaveBuffer waveBuffer = new WaveBuffer(buffer);
            short expected = 0;
            for (int sample = 0; sample < samples; sample++)
            {
                short sampleVal = waveBuffer.ShortBuffer[sample];
                Assert.AreEqual(expected--, sampleVal, "sample #" + sample.ToString());
            }
        }
    }

    class TestStereoProvider : WaveProvider16
    {
        public TestStereoProvider()
            : base(44100, 2)
        { }

        short current;

        public override int Read(short[] buffer, int offset, int sampleCount)
        {
            for (int sample = 0; sample < sampleCount; sample+=2)
            {
                buffer[offset + sample] = current;
                buffer[offset + sample + 1] = (short)(0 - current);
                current++;
            }
            return sampleCount;
        }
    }
}
