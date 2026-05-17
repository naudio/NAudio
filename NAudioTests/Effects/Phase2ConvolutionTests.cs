using System;
using NAudio.Dsp;
using NAudio.Effects;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Effects
{
    [TestFixture]
    [Category("UnitTest")]
    public class PartitionedConvolverTests
    {
        private static float[] NaiveConvolution(float[] x, float[] h)
        {
            var y = new float[x.Length + h.Length - 1];
            for (var n = 0; n < x.Length; n++)
                for (var k = 0; k < h.Length; k++)
                    y[n + k] += x[n] * h[k];
            return y;
        }

        private static float[] RunStream(PartitionedConvolver c, float[] input, int flush)
        {
            var output = new float[input.Length + flush];
            for (var i = 0; i < input.Length; i++)
                output[i] = c.Process(input[i]);
            for (var i = 0; i < flush; i++)
                output[input.Length + i] = c.Process(0f);
            return output;
        }

        [Test]
        public void UnitImpulseResponseIsADelayOfOnePartition()
        {
            var c = new PartitionedConvolver(new[] { 1f }, 64);
            var input = new float[300];
            for (var i = 0; i < input.Length; i++)
                input[i] = MathF.Sin(i * 0.2f);

            var output = RunStream(c, input, 128);

            for (var i = 0; i < input.Length; i++)
                Assert.That(output[i + c.LatencySamples], Is.EqualTo(input[i]).Within(1e-3f));
        }

        [Test]
        public void ShortKernelMatchesDirectConvolution()
        {
            var h = new[] { 0.5f, -0.25f, 0.125f, 0.0625f };
            var c = new PartitionedConvolver(h, 64);

            var x = new float[200];
            var rng = new Random(1);
            for (var i = 0; i < x.Length; i++)
                x[i] = (float)(rng.NextDouble() * 2.0 - 1.0);

            var output = RunStream(c, x, 256);
            var expected = NaiveConvolution(x, h);

            for (var n = 0; n < expected.Length; n++)
                Assert.That(output[n + c.LatencySamples], Is.EqualTo(expected[n]).Within(2e-3f));
        }

        [Test]
        public void MultiPartitionKernelMatchesDirectConvolution()
        {
            var h = new float[200]; // spans 4 partitions of 64
            for (var i = 0; i < h.Length; i++)
                h[i] = MathF.Exp(-i * 0.02f) * MathF.Sin(i * 0.3f);
            var c = new PartitionedConvolver(h, 64);

            var x = new float[512];
            var rng = new Random(2);
            for (var i = 0; i < x.Length; i++)
                x[i] = (float)(rng.NextDouble() * 2.0 - 1.0);

            var output = RunStream(c, x, h.Length + 256);
            var expected = NaiveConvolution(x, h);

            for (var n = 0; n < expected.Length; n++)
                Assert.That(output[n + c.LatencySamples], Is.EqualTo(expected[n]).Within(5e-3f));
        }

        [Test]
        public void ConstructorValidatesArguments()
        {
            Assert.Throws<ArgumentException>(() => new PartitionedConvolver(new[] { 1f }, 100));
            Assert.Throws<ArgumentException>(() => new PartitionedConvolver(Array.Empty<float>(), 64));
        }
    }

    [TestFixture]
    [Category("UnitTest")]
    public class ConvolutionReverbEffectTests
    {
        private static WaveFormat Stereo => WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

        [Test]
        public void NoImpulseResponseIsPassThrough()
        {
            var fx = new ConvolutionReverbEffect();
            fx.Configure(Stereo);

            var buffer = new[] { 0.1f, -0.2f, 0.3f, -0.4f };
            var expected = (float[])buffer.Clone();
            fx.Process(buffer);

            Assert.That(buffer, Is.EqualTo(expected));
            Assert.That(fx.LatencySamples, Is.EqualTo(0));
        }

        [Test]
        public void AppliesImpulseResponsePerChannel()
        {
            var fx = new ConvolutionReverbEffect { PartitionSize = 64, Mix = 1f };
            fx.SetImpulseResponse(new[] { 1f });
            fx.Configure(Stereo);

            Assert.That(fx.LatencySamples, Is.EqualTo(64));

            // Interleaved stereo impulse on both channels.
            var frames = 512;
            var buffer = new float[frames * 2];
            buffer[0] = 1f;
            buffer[1] = 1f;
            fx.Process(buffer);

            // A unit IR is a pure delay of one partition (64 frames).
            Assert.That(buffer[64 * 2], Is.EqualTo(1f).Within(1e-3f));
            Assert.That(buffer[64 * 2 + 1], Is.EqualTo(1f).Within(1e-3f));
            Assert.That(MathF.Abs(buffer[10 * 2]), Is.LessThan(1e-3f));
        }

        [Test]
        public void PartitionSizeMustBePowerOfTwo()
        {
            var fx = new ConvolutionReverbEffect();
            Assert.Throws<ArgumentException>(() => fx.PartitionSize = 200);
        }
    }
}
