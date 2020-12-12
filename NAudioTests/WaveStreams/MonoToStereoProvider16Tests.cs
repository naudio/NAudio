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
            var buffer = new Span<byte>(new byte[samples * 2]);
            int read = stereo.Read(buffer);
            Assert.AreEqual(buffer.Length, read, "bytes read");
            var shortBuffer = MemoryMarshal.Cast<byte,short>(buffer);
            short expected = 0;
            for (int sample = 0; sample < samples; sample+=2)
            {
                short sampleLeft = shortBuffer[sample];
                short sampleRight = shortBuffer[sample+1];
                Assert.AreEqual(expected++, sampleLeft, "sample left");
                Assert.AreEqual(0, sampleRight, "sample right");
            }
        }
    }


    class TestMonoProvider : WaveProvider16
    {
        short current;

        public override int Read(Span<short> buffer)
        {
            for (int sample = 0; sample < buffer.Length; sample++)
            {
                buffer[sample] = current++;
            }
            return buffer.Length;
        }
    }
}

