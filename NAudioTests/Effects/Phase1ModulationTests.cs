using System;
using NAudio.Dsp;
using NAudio.Effects;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Effects
{
    [TestFixture]
    [Category("UnitTest")]
    public class TempoTimeTests
    {
        [Test]
        public void QuarterNoteAt120BpmIsHalfASecond()
        {
            Assert.That(TempoTime.Seconds(120.0, NoteDivision.Quarter), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(TempoTime.Hertz(120.0, NoteDivision.Quarter), Is.EqualTo(2.0).Within(1e-12));
        }

        [Test]
        public void DottedAndTripletScale()
        {
            Assert.That(TempoTime.Seconds(120.0, NoteDivision.DottedQuarter), Is.EqualTo(0.75).Within(1e-12));
            Assert.That(TempoTime.Seconds(120.0, NoteDivision.TripletEighth), Is.EqualTo(0.5 / 3.0).Within(1e-12));
        }

        [Test]
        public void RejectsNonPositiveTempo()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TempoTime.Seconds(0.0, NoteDivision.Quarter));
        }
    }

    [TestFixture]
    [Category("UnitTest")]
    public class LfoTests
    {
        [Test]
        public void SineStaysInRange()
        {
            var lfo = new Lfo(48000) { FrequencyHz = 100f };
            for (var i = 0; i < 5000; i++)
            {
                var v = lfo.Process();
                Assert.That(v, Is.InRange(-1.0001f, 1.0001f));
            }
        }

        [Test]
        public void SquareIsBipolarBinary()
        {
            var lfo = new Lfo(48000) { Waveform = LfoWaveform.Square, FrequencyHz = 50f };
            for (var i = 0; i < 2000; i++)
            {
                var v = lfo.Process();
                Assert.That(v == 1f || v == -1f, Is.True);
            }
        }

        [Test]
        public void SampleAndHoldIsDeterministic()
        {
            var a = new Lfo(48000) { Waveform = LfoWaveform.SampleAndHold, FrequencyHz = 200f };
            var b = new Lfo(48000) { Waveform = LfoWaveform.SampleAndHold, FrequencyHz = 200f };
            for (var i = 0; i < 1000; i++)
                Assert.That(a.Process(), Is.EqualTo(b.Process()));
        }

        [Test]
        public void SyncToTempoSetsFrequency()
        {
            var lfo = new Lfo(48000);
            lfo.SyncToTempo(120.0, NoteDivision.Quarter);
            Assert.That(lfo.FrequencyHz, Is.EqualTo(2f).Within(1e-4f));
        }

        [Test]
        public void RejectsNonPositiveFrequency()
        {
            var lfo = new Lfo(48000);
            Assert.Throws<ArgumentOutOfRangeException>(() => lfo.FrequencyHz = 0f);
        }
    }

    [TestFixture]
    [Category("UnitTest")]
    public class DelayEffectTests
    {
        private static WaveFormat Mono => WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);

        [Test]
        public void ProducesADelayedEcho()
        {
            var delay = new DelayEffect { DelayMilliseconds = 10f, Feedback = 0f, Mix = 0.5f };
            delay.Configure(Mono);

            var buffer = new float[2048];
            buffer[0] = 1f;
            delay.Process(buffer);

            // 10 ms @ 48 kHz ≈ 480 samples.
            float echo = 0f;
            for (var i = 470; i <= 490; i++)
                echo = MathF.Max(echo, MathF.Abs(buffer[i]));

            Assert.That(echo, Is.GreaterThan(0.2f));
            Assert.That(MathF.Abs(buffer[5]), Is.LessThan(0.05f));
        }

        [Test]
        public void TempoSyncRunsCleanly()
        {
            var delay = new DelayEffect { TempoSync = true, Bpm = 120, Division = NoteDivision.Sixteenth };
            delay.Configure(Mono);

            var buffer = new float[4096];
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = MathF.Sin(i * 0.05f);
            delay.Process(buffer);

            foreach (var s in buffer)
                Assert.That(float.IsFinite(s), Is.True);
        }
    }

    [TestFixture]
    [Category("UnitTest")]
    public class ModulationEffectsTests
    {
        private static WaveFormat Mono => WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);
        private static WaveFormat Stereo => WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

        private static float[] Sine(int n, float amp = 0.5f)
        {
            var b = new float[n];
            for (var i = 0; i < n; i++)
                b[i] = amp * MathF.Sin(i * 0.1f);
            return b;
        }

        [Test]
        public void ChorusAltersSignalCleanly()
        {
            var fx = new ChorusEffect();
            fx.Configure(Mono);
            var input = Sine(4096);
            var buffer = (float[])input.Clone();
            fx.Process(buffer);

            var different = false;
            for (var i = 2048; i < buffer.Length; i++)
            {
                Assert.That(float.IsFinite(buffer[i]), Is.True);
                if (MathF.Abs(buffer[i] - input[i]) > 1e-4f)
                    different = true;
            }
            Assert.That(different, Is.True);
        }

        [Test]
        public void FlangerRunsCleanly()
        {
            var fx = new FlangerEffect { Feedback = -0.4f };
            fx.Configure(Mono);
            var buffer = Sine(4096);
            fx.Process(buffer);
            foreach (var s in buffer)
                Assert.That(float.IsFinite(s), Is.True);
        }

        [Test]
        public void PhaserRunsCleanlyAndValidatesStages()
        {
            var fx = new PhaserEffect { Stages = 6 };
            fx.Configure(Mono);
            var buffer = Sine(4096);
            fx.Process(buffer);
            foreach (var s in buffer)
                Assert.That(float.IsFinite(s), Is.True);

            Assert.Throws<ArgumentOutOfRangeException>(() => fx.Stages = 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => fx.Stages = 25);
        }

        [Test]
        public void TremoloModulatesAmplitude()
        {
            var fx = new TremoloEffect { Depth = 1f, RateHz = 20f };
            fx.Configure(Mono);

            var buffer = new float[9600];
            Array.Fill(buffer, 1f);
            fx.Process(buffer);

            float min = float.MaxValue, max = float.MinValue;
            foreach (var s in buffer)
            {
                min = MathF.Min(min, s);
                max = MathF.Max(max, s);
            }
            Assert.That(min, Is.LessThan(0.2f));
            Assert.That(max, Is.GreaterThan(0.8f));
        }

        [Test]
        public void TremoloAutoPanIsFiniteOnStereo()
        {
            var fx = new TremoloEffect { AutoPan = true, RateHz = 10f };
            fx.Configure(Stereo);

            var buffer = new float[4096];
            Array.Fill(buffer, 0.5f);
            fx.Process(buffer);

            foreach (var s in buffer)
                Assert.That(float.IsFinite(s), Is.True);
        }
    }
}
