using System;
using System.IO;
using System.Runtime.InteropServices;
using NAudio.Utils;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    public class WaveMixerStream32Tests
    {
        /// <summary>
        /// Build an in-memory IEEE-float stereo WAV filled with a constant sample value,
        /// then wrap it in a WaveFileReader so it can be fed straight into the mixer.
        /// </summary>
        private static WaveFileReader CreateConstantFloatStream(float sampleValue, int frames, int sampleRate = 44100)
        {
            var ms = new MemoryStream();
            using (var writer = new WaveFileWriter(new IgnoreDisposeStream(ms),
                       WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2)))
            {
                for (int i = 0; i < frames; i++)
                {
                    writer.WriteSample(sampleValue);
                    writer.WriteSample(sampleValue);
                }
            }
            return new WaveFileReader(new MemoryStream(ms.ToArray()));
        }

        [Test]
        public void MixingThreeConstantStreams_ProducesArithmeticSumPerSample()
        {
            const int frames = 512;
            using var mixer = new WaveMixerStream32 { AutoStop = true };
            mixer.AddInputStream(CreateConstantFloatStream(0.1f, frames));
            mixer.AddInputStream(CreateConstantFloatStream(0.2f, frames));
            mixer.AddInputStream(CreateConstantFloatStream(0.3f, frames));

            var buffer = new byte[(int)mixer.Length];
            int read = mixer.Read(buffer, 0, buffer.Length);
            Assert.That(read, Is.EqualTo(buffer.Length));

            var floats = MemoryMarshal.Cast<byte, float>(buffer.AsSpan()).ToArray();
            const float expected = 0.1f + 0.2f + 0.3f;
            foreach (var f in floats)
            {
                Assert.That(f, Is.EqualTo(expected).Within(1e-6f));
            }
        }

        [Test]
        public void ReadWithCountNotMultipleOfBytesPerSample_Throws()
        {
            using var mixer = new WaveMixerStream32 { AutoStop = false };
            mixer.AddInputStream(CreateConstantFloatStream(0.1f, 512));

            // bytesPerSample is 4 for IEEE float — 7 is deliberately misaligned.
            var buffer = new byte[8];
            Assert.Throws<ArgumentException>(() => { _ = mixer.Read(buffer, 0, 7); });
        }

    }
}
