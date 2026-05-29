using System;
using NAudio.Dsp;
using NAudio.Effects;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Effects
{
    [TestFixture]
    [Category("UnitTest")]
    public class ParameterSmootherTests
    {
        [Test]
        public void RampsTowardsTargetAndSettles()
        {
            var smoother = new ParameterSmoother();
            smoother.Configure(48000, 10f);
            smoother.Reset(0f);
            smoother.SetTarget(1f);

            for (var i = 0; i < 48000; i++)
                smoother.Process();

            Assert.That(smoother.Current, Is.EqualTo(1f).Within(1e-3f));
            Assert.That(smoother.IsSettled, Is.True);
        }

        [Test]
        public void ResetJumpsImmediatelyWithNoRamp()
        {
            var smoother = new ParameterSmoother();
            smoother.Configure(44100);
            smoother.Reset(0.5f);

            Assert.That(smoother.Current, Is.EqualTo(0.5f));
            Assert.That(smoother.Target, Is.EqualTo(0.5f));
            Assert.That(smoother.IsSettled, Is.True);
        }

        [Test]
        public void ConfigureRejectsNonPositiveArguments()
        {
            var smoother = new ParameterSmoother();
            Assert.Throws<ArgumentOutOfRangeException>(() => smoother.Configure(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => smoother.Configure(48000, 0f));
        }
    }

    [TestFixture]
    [Category("UnitTest")]
    public class EnvelopeFollowerTests
    {
        [Test]
        public void RisesQuicklyOnLoudInputThenDecaysOnSilence()
        {
            var follower = new EnvelopeFollower(1f, 200f, 48000);

            for (var i = 0; i < 4800; i++) // 100 ms of full-scale input
                follower.ProcessSample(1f);
            var attacked = follower.Envelope;

            for (var i = 0; i < 480; i++) // 10 ms of silence
                follower.ProcessSample(0f);
            var released = follower.Envelope;

            Assert.That(attacked, Is.GreaterThan(0.95f));
            Assert.That(released, Is.LessThan(attacked));
            Assert.That(released, Is.GreaterThan(0f));
        }

        [Test]
        public void ProcessSampleRectifiesNegativeInput()
        {
            var a = new EnvelopeFollower(5f, 50f, 48000);
            var b = new EnvelopeFollower(5f, 50f, 48000);

            for (var i = 0; i < 100; i++)
            {
                var viaNegative = a.ProcessSample(-0.7f);
                var viaRectified = b.ProcessRectified(0.7f);
                Assert.That(viaNegative, Is.EqualTo(viaRectified).Within(1e-6f));
            }
        }

        [Test]
        public void ConstructorRejectsNonPositiveArguments()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnvelopeFollower(0f, 10f, 48000));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnvelopeFollower(10f, 0f, 48000));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnvelopeFollower(10f, 10f, 0));
        }
    }

    [TestFixture]
    [Category("UnitTest")]
    public class DelayLineTests
    {
        [Test]
        public void IntegerReadReturnsDelayedSamples()
        {
            var line = new DelayLine(8);
            line.Write(1f);
            line.Write(2f);
            line.Write(3f);

            Assert.That(line.Read(1), Is.EqualTo(3f));
            Assert.That(line.Read(2), Is.EqualTo(2f));
            Assert.That(line.Read(3), Is.EqualTo(1f));
        }

        [Test]
        public void FractionalReadLinearlyInterpolates()
        {
            var line = new DelayLine(8);
            line.Write(0f);
            line.Write(10f);

            Assert.That(line.Read(1f), Is.EqualTo(10f).Within(1e-6f));
            Assert.That(line.Read(2f), Is.EqualTo(0f).Within(1e-6f));
            Assert.That(line.Read(1.5f), Is.EqualTo(5f).Within(1e-6f));
        }

        [Test]
        public void ResetClearsContents()
        {
            var line = new DelayLine(4);
            line.Write(9f);
            line.Reset();
            line.Write(0f);
            Assert.That(line.Read(2), Is.EqualTo(0f));
        }

        [Test]
        public void OutOfRangeReadsThrow()
        {
            var line = new DelayLine(4);
            Assert.Throws<ArgumentOutOfRangeException>(() => line.Read(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => line.Read(5));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DelayLine(0));
        }
    }

    [TestFixture]
    [Category("UnitTest")]
    public class CrossfadingBiQuadFilterTests
    {
        private const float SampleRate = 48000f;

        [Test]
        public void NotCrossfadingMatchesAStandaloneFilter()
        {
            var reference = BiQuadFilter.LowPassFilter(SampleRate, 1000f, 0.707f);
            var crossfading = new CrossfadingBiQuadFilter(
                BiQuadFilter.LowPassFilter(SampleRate, 1000f, 0.707f),
                BiQuadFilter.LowPassFilter(SampleRate, 1000f, 0.707f),
                64);

            Assert.That(crossfading.IsCrossfading, Is.False);
            for (var i = 0; i < 256; i++)
            {
                var input = MathF.Sin(i * 0.1f);
                Assert.That(crossfading.Transform(input),
                    Is.EqualTo(reference.Transform(input)).Within(1e-6f));
            }
        }

        [Test]
        public void CrossfadeCompletesAfterTheConfiguredLength()
        {
            var crossfading = new CrossfadingBiQuadFilter(
                BiQuadFilter.LowPassFilter(SampleRate, 1000f, 0.707f),
                BiQuadFilter.LowPassFilter(SampleRate, 1000f, 0.707f),
                32);

            crossfading.Standby.SetLowPassFilter(SampleRate, 4000f, 0.707f);
            crossfading.BeginCrossfade();
            Assert.That(crossfading.IsCrossfading, Is.True);

            for (var i = 0; i < 32; i++)
            {
                var output = crossfading.Transform(0.5f);
                Assert.That(float.IsFinite(output), Is.True);
            }

            Assert.That(crossfading.IsCrossfading, Is.False);
        }

        [Test]
        public void ConstructorValidatesArguments()
        {
            var a = BiQuadFilter.LowPassFilter(SampleRate, 1000f, 0.707f);
            var b = BiQuadFilter.LowPassFilter(SampleRate, 1000f, 0.707f);
            Assert.Throws<ArgumentNullException>(() => new CrossfadingBiQuadFilter(null, b, 16));
            Assert.Throws<ArgumentNullException>(() => new CrossfadingBiQuadFilter(a, null, 16));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CrossfadingBiQuadFilter(a, b, 0));
        }
    }

    [TestFixture]
    [Category("UnitTest")]
    public class AudioEffectTests
    {
        private sealed class DoublingEffect : AudioEffect
        {
            protected override void OnConfigure(WaveFormat format)
            {
            }

            protected override void ProcessBlock(Span<float> buffer)
            {
                for (var i = 0; i < buffer.Length; i++)
                    buffer[i] *= 2f;
            }
        }

        private static WaveFormat MonoFloat => WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);

        [Test]
        public void FullyWetByDefaultAppliesTheEffect()
        {
            var effect = new DoublingEffect();
            effect.Configure(MonoFloat);

            Span<float> buffer = stackalloc float[64];
            buffer.Fill(1f);
            effect.Process(buffer);

            foreach (var sample in buffer)
                Assert.That(sample, Is.EqualTo(2f));
        }

        [Test]
        public void BypassRampsTowardsTheDrySignal()
        {
            var effect = new DoublingEffect();
            effect.Configure(MonoFloat);
            effect.Bypass = true;

            var buffer = new float[480];
            float last = 0f;
            for (var block = 0; block < 60; block++) // ~0.6 s of audio
            {
                Array.Fill(buffer, 1f);
                effect.Process(buffer);
                last = buffer[^1];
            }

            Assert.That(last, Is.EqualTo(1f).Within(1e-3f));
        }

        [Test]
        public void ZeroMixIsFullyDry()
        {
            var effect = new DoublingEffect { Mix = 0f };
            effect.Configure(MonoFloat);

            var buffer = new float[32];
            Array.Fill(buffer, 0.3f);
            effect.Process(buffer);

            foreach (var sample in buffer)
                Assert.That(sample, Is.EqualTo(0.3f).Within(1e-6f));
        }

        [Test]
        public void ProcessBeforeConfigureThrows()
        {
            var effect = new DoublingEffect();
            Assert.Throws<InvalidOperationException>(() => effect.Process(new float[16]));
        }
    }

    [TestFixture]
    [Category("UnitTest")]
    public class EffectChainTests
    {
        private sealed class ConstantProvider : ISampleProvider
        {
            private readonly float value;

            public ConstantProvider(WaveFormat waveFormat, float value)
            {
                WaveFormat = waveFormat;
                this.value = value;
            }

            public WaveFormat WaveFormat { get; }

            public int Read(Span<float> buffer)
            {
                buffer.Fill(value);
                return buffer.Length;
            }
        }

        private sealed class DoublingEffect : AudioEffect
        {
            protected override void OnConfigure(WaveFormat format)
            {
            }

            protected override void ProcessBlock(Span<float> buffer)
            {
                for (var i = 0; i < buffer.Length; i++)
                    buffer[i] *= 2f;
            }
        }

        // One-sample delay: carries the previous input across blocks, and clears that
        // carried state on Reset — lets tests observe whether Reset reached the effect.
        private sealed class OneSampleDelayEffect : AudioEffect
        {
            private float last;

            protected override void OnConfigure(WaveFormat format)
            {
            }

            protected override void ProcessBlock(Span<float> buffer)
            {
                for (var i = 0; i < buffer.Length; i++)
                {
                    var current = buffer[i];
                    buffer[i] = last;
                    last = current;
                }
            }

            public override void Reset()
            {
                base.Reset();
                last = 0f;
            }
        }

        // Adds a fixed amount; lets tests assert on chain order by the resulting offset.
        private sealed class AddConstantEffect : AudioEffect
        {
            private readonly float amount;

            public AddConstantEffect(float amount) => this.amount = amount;

            protected override void OnConfigure(WaveFormat format)
            {
            }

            protected override void ProcessBlock(Span<float> buffer)
            {
                for (var i = 0; i < buffer.Length; i++)
                    buffer[i] += amount;
            }
        }

        private static WaveFormat MonoFloat => WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);

        [Test]
        public void EffectSampleProviderAppliesTheEffect()
        {
            var source = new ConstantProvider(MonoFloat, 1f);
            var provider = new EffectSampleProvider(source, new DoublingEffect());

            var buffer = new float[16];
            var read = provider.Read(buffer);

            Assert.That(read, Is.EqualTo(16));
            foreach (var sample in buffer)
                Assert.That(sample, Is.EqualTo(2f));
        }

        [Test]
        public void ChainAppliesEffectsInOrderAndSumsLatency()
        {
            var source = new ConstantProvider(MonoFloat, 1f);
            var chain = new EffectChain(source)
                .Add(new DoublingEffect())
                .Add(new DoublingEffect());

            var buffer = new float[16];
            chain.Read(buffer);

            Assert.That(chain.Effects, Has.Count.EqualTo(2));
            Assert.That(chain.LatencySamples, Is.EqualTo(0));
            foreach (var sample in buffer)
                Assert.That(sample, Is.EqualTo(4f));
        }

        [Test]
        public void InsertPlacesEffectAtIndexAndIsConfigured()
        {
            var source = new ConstantProvider(MonoFloat, 1f);
            var chain = new EffectChain(source).Add(new DoublingEffect());
            chain.Insert(0, new AddConstantEffect(1f)); // (1 + 1) * 2 = 4

            var buffer = new float[8];
            chain.Read(buffer);

            Assert.That(chain.Effects, Has.Count.EqualTo(2));
            foreach (var sample in buffer)
                Assert.That(sample, Is.EqualTo(4f));
        }

        [Test]
        public void RemoveAtDropsTheEffectFromProcessing()
        {
            var source = new ConstantProvider(MonoFloat, 1f);
            var chain = new EffectChain(source)
                .Add(new DoublingEffect())
                .Add(new AddConstantEffect(1f)); // (1 * 2) + 1 = 3

            chain.RemoveAt(1); // leaves just the doubling effect → 2

            var buffer = new float[8];
            chain.Read(buffer);

            Assert.That(chain.Effects, Has.Count.EqualTo(1));
            foreach (var sample in buffer)
                Assert.That(sample, Is.EqualTo(2f));
        }

        [Test]
        public void MoveReordersEffects()
        {
            var source = new ConstantProvider(MonoFloat, 1f);
            var chain = new EffectChain(source)
                .Add(new DoublingEffect())
                .Add(new AddConstantEffect(1f)); // [Double, Add1] → (1 * 2) + 1 = 3

            chain.Move(0, 1); // [Add1, Double] → (1 + 1) * 2 = 4

            var buffer = new float[8];
            chain.Read(buffer);

            foreach (var sample in buffer)
                Assert.That(sample, Is.EqualTo(4f));
        }

        [Test]
        public void EditsValidateIndices()
        {
            var source = new ConstantProvider(MonoFloat, 1f);
            var chain = new EffectChain(source).Add(new DoublingEffect());

            Assert.Throws<ArgumentNullException>(() => chain.Add(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => chain.Insert(2, new DoublingEffect()));
            Assert.Throws<ArgumentOutOfRangeException>(() => chain.RemoveAt(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => chain.Move(0, 1));
        }

        [Test]
        public void ResetClearsEachEffectsState()
        {
            var source = new ConstantProvider(MonoFloat, 1f);
            var chain = new EffectChain(source).Add(new OneSampleDelayEffect());

            var buffer = new float[4];
            chain.Read(buffer); // primes the delay: out [0,1,1,1], carried sample = 1

            chain.Read(buffer); // carries state across the block boundary
            Assert.That(buffer[0], Is.EqualTo(1f), "carried state should surface without a reset");

            chain.Reset();
            Array.Clear(buffer, 0, buffer.Length);
            chain.Read(buffer); // state cleared, so the first sample is the initial 0 again
            Assert.That(buffer[0], Is.EqualTo(0f), "Reset should clear the effect's carried state");
        }

        [Test]
        public void EditingWhileReadingIsThreadSafe()
        {
            var source = new ConstantProvider(MonoFloat, 0.1f);
            var chain = new EffectChain(source).Add(new DoublingEffect());

            var stop = false;
            Exception readerError = null;
            var reader = new System.Threading.Thread(() =>
            {
                try
                {
                    var buffer = new float[256];
                    while (!System.Threading.Volatile.Read(ref stop))
                    {
                        chain.Read(buffer);
                        foreach (var sample in buffer)
                            if (!float.IsFinite(sample))
                                throw new InvalidOperationException("non-finite sample");
                    }
                }
                catch (Exception ex)
                {
                    readerError = ex;
                }
            });
            reader.Start();

            for (var i = 0; i < 2000; i++)
            {
                chain.Add(new AddConstantEffect(0.01f));
                if (chain.Effects.Count > 1)
                    chain.Move(0, chain.Effects.Count - 1);
                chain.RemoveAt(chain.Effects.Count - 1);
            }

            System.Threading.Volatile.Write(ref stop, true);
            reader.Join();

            Assert.That(readerError, Is.Null);
        }
    }
}
