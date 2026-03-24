using System;
using System.Runtime.InteropServices;
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
            IAudioSource monoStream = new TestMonoProvider();
            MonoToStereoProvider16 stereo = new MonoToStereoProvider16(monoStream);
            stereo.LeftVolume = 1.0f;
            stereo.RightVolume = 0.0f;
            int samples = 1000;
            byte[] buffer = new byte[samples * 2];
            int read = stereo.Read(buffer.AsSpan());
            Assert.That(read, Is.EqualTo(buffer.Length), "bytes read");
            var shortBuffer = MemoryMarshal.Cast<byte, short>(buffer.AsSpan());
            short expected = 0;
            for (int sample = 0; sample < samples; sample+=2)
            {
                short sampleLeft = shortBuffer[sample];
                short sampleRight = shortBuffer[sample+1];
                Assert.That(sampleLeft, Is.EqualTo(expected++), "sample left");
                Assert.That(sampleRight, Is.EqualTo(0), "sample right");
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
