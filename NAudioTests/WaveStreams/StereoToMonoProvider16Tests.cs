using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.Wave;
using System.Runtime.InteropServices;

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
            var buffer = new Span<byte>(new byte[samples * 2]);
            int read = mono.Read(buffer);
            Assert.AreEqual(buffer.Length, read, "bytes read");
            var shortBuffer = MemoryMarshal.Cast<byte,short>(buffer);
            short expected = 0;
            for (int sample = 0; sample < samples; sample++)
            {
                short sampleVal = shortBuffer[sample];
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

        public override int Read(Span<short> buffer)
        {
            for (int sample = 0; sample < buffer.Length; sample+=2)
            {
                buffer[sample] = current;
                buffer[sample + 1] = (short)(0 - current);
                current++;
            }
            return buffer.Length;
        }
    }
}
