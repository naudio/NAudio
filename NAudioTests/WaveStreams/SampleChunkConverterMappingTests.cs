using System;
using NAudio.Utils;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    [Category("UnitTest")]
    public class SampleChunkConverterMappingTests
    {
        [Test]
        public void Mono16ConverterMapsPcmToStereoFloat()
        {
            var (bytesRead, left, right) = ReadStereoFrame(Pcm16(16384), new WaveFormat(8000, 16, 1));

            Assert.That(bytesRead, Is.EqualTo(8));
            Assert.That(left, Is.EqualTo(0.5f).Within(1e-6f));
            Assert.That(right, Is.EqualTo(0.5f).Within(1e-6f));
        }

        [Test]
        public void Stereo16ConverterMapsPcmToStereoFloat()
        {
            var (bytesRead, left, right) = ReadStereoFrame(Pcm16(-32768, 16384), new WaveFormat(8000, 16, 2));

            Assert.That(bytesRead, Is.EqualTo(8));
            Assert.That(left, Is.EqualTo(-1.0f).Within(1e-6f));
            Assert.That(right, Is.EqualTo(0.5f).Within(1e-6f));
        }

        [Test]
        public void Mono24ConverterMapsPcmToStereoFloat()
        {
            var (bytesRead, left, right) = ReadStereoFrame(Pcm24(4194304), new WaveFormat(8000, 24, 1));

            Assert.That(bytesRead, Is.EqualTo(8));
            Assert.That(left, Is.EqualTo(0.5f).Within(1e-6f));
            Assert.That(right, Is.EqualTo(0.5f).Within(1e-6f));
        }

        [Test]
        public void Stereo24ConverterMapsPcmToStereoFloat()
        {
            var (bytesRead, left, right) = ReadStereoFrame(Pcm24(-8388608, 8388607), new WaveFormat(8000, 24, 2));

            Assert.That(bytesRead, Is.EqualTo(8));
            Assert.That(left, Is.EqualTo(-1.0f).Within(1e-6f));
            Assert.That(right, Is.EqualTo(8388607f / 8388608f).Within(1e-6f));
        }

        [Test]
        public void MonoFloatConverterMapsToStereoFloat()
        {
            var (bytesRead, left, right) = ReadStereoFrame(IeeeFloat(0.25f), WaveFormat.CreateIeeeFloatWaveFormat(8000, 1));

            Assert.That(bytesRead, Is.EqualTo(8));
            Assert.That(left, Is.EqualTo(0.25f).Within(1e-6f));
            Assert.That(right, Is.EqualTo(0.25f).Within(1e-6f));
        }

        [Test]
        public void StereoFloatConverterMapsToStereoFloat()
        {
            var (bytesRead, left, right) = ReadStereoFrame(IeeeFloat(-0.75f, 0.125f), WaveFormat.CreateIeeeFloatWaveFormat(8000, 2));

            Assert.That(bytesRead, Is.EqualTo(8));
            Assert.That(left, Is.EqualTo(-0.75f).Within(1e-6f));
            Assert.That(right, Is.EqualTo(0.125f).Within(1e-6f));
        }

        [Test]
        public void Mono8ConverterShouldMapUnsignedPcmAroundZero()
        {
            var (bytesRead, left, right) = ReadStereoFrame(new byte[] { 0 }, new WaveFormat(8000, 8, 1));

            Assert.That(bytesRead, Is.EqualTo(8));
            Assert.That(left, Is.EqualTo(-1.0f).Within(1e-6f));
            Assert.That(right, Is.EqualTo(-1.0f).Within(1e-6f));
        }

        [Test]
        public void Stereo8ConverterShouldMapUnsignedPcmAroundZero()
        {
            var (bytesRead, left, right) = ReadStereoFrame(new byte[] { 0, 255 }, new WaveFormat(8000, 8, 2));

            Assert.That(bytesRead, Is.EqualTo(8));
            Assert.That(left, Is.EqualTo(-1.0f).Within(1e-6f));
            Assert.That(right, Is.EqualTo(255f / 128f - 1f).Within(1e-6f));
        }

        private static (int bytesRead, float left, float right) ReadStereoFrame(byte[] sourceBytes, WaveFormat waveFormat)
        {
            var source = new RawSourceWaveStream(sourceBytes, 0, sourceBytes.Length, waveFormat);
            using (var waveChannel = new WaveChannel32(source) { PadWithZeroes = false })
            {
                var dest = new byte[8];
                var bytesRead = waveChannel.Read(dest, 0, dest.Length);
                var buffer = new WaveBuffer(dest);
                return (bytesRead, buffer.FloatBuffer[0], buffer.FloatBuffer[1]);
            }
        }

        private static byte[] Pcm16(params short[] samples)
        {
            var bytes = new byte[samples.Length * 2];
            Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private static byte[] Pcm24(params int[] samples)
        {
            var bytes = new byte[samples.Length * 3];
            for (var n = 0; n < samples.Length; n++)
            {
                var sample = samples[n];
                var offset = n * 3;
                bytes[offset] = (byte)(sample & 0xFF);
                bytes[offset + 1] = (byte)((sample >> 8) & 0xFF);
                bytes[offset + 2] = (byte)((sample >> 16) & 0xFF);
            }
            return bytes;
        }

        private static byte[] IeeeFloat(params float[] samples)
        {
            var bytes = new byte[samples.Length * 4];
            Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
            return bytes;
        }
    }
}
