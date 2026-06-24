using System;
using NAudio.Effects;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Effects
{
    /// <summary>
    /// The Reset() safety net the code review flagged as missing: ~30 Reset()
    /// overrides were untested. Each effect is run on a deterministic signal, then
    /// Reset(), then run on the same signal again — a correct Reset() clears every
    /// piece of state (delay lines, filter histories, envelopes, reverb tails,
    /// adaptive estimates, RNGs) so the second pass is identical to the first.
    /// Buffers are kept to 0.5 s so the whole net stays in the fast per-build run.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class EffectResetTests
    {
        private const int Sr = 48000;
        private const int N = Sr / 2; // 0.5 s — long enough for delay/reverb residue to surface

        private static WaveFormat Mono => WaveFormat.CreateIeeeFloatWaveFormat(Sr, 1);

        // Deterministic broadband excitation: two tones plus a short opening burst
        // to kick feedback paths.
        private static float[] Excite()
        {
            var b = new float[N];
            for (var i = 0; i < N; i++)
                b[i] = 0.4f * MathF.Sin(i * (2f * MathF.PI * 220f / Sr))
                     + 0.2f * MathF.Sin(i * (2f * MathF.PI * 1750f / Sr));
            for (var i = 0; i < 64; i++)
                b[i] += 0.5f;
            return b;
        }

        private static void AssertResetIsClean(AudioEffect effect)
        {
            effect.Mix = 1f;
            effect.Configure(Mono);
            var input = Excite();

            var first = (float[])input.Clone();
            effect.Process(first);

            effect.Reset();

            var second = (float[])input.Clone();
            effect.Process(second);

            for (var i = 0; i < N; i++)
            {
                Assert.That(float.IsFinite(second[i]), Is.True, $"non-finite at {i}");
                Assert.That(second[i], Is.EqualTo(first[i]).Within(1e-4f),
                    $"{effect.GetType().Name}: Reset() left residual state (sample {i})");
            }
        }

        private static AudioEffect[] AllEffects() => new AudioEffect[]
        {
            new GainEffect(), new PanEffect(), new StereoWidthEffect(),
            new CompressorEffect(), new LimiterEffect(), new GateEffect(),
            new SaturationEffect(), new DelayEffect(), new TremoloEffect(),
            new ReverbEffect(), new FdnReverbEffect(), new NoiseSuppressionEffect(),
            new PitchShiftEffect(), new ChorusEffect(), new FlangerEffect(),
            new PhaserEffect(), new BitCrusherEffect(), new DcBlockerEffect(),
            new MonoMakerEffect(), new AutomaticGainControlEffect(),
            new TransientShaperEffect(), new DeEsserEffect(), new ComfortNoiseEffect()
        };

        [Test]
        public void EveryEffectFullyResets([ValueSource(nameof(AllEffects))] AudioEffect effect)
            => AssertResetIsClean(effect);

        [Test]
        public void EqualizerFullyResets()
            => AssertResetIsClean(new Equalizer(
                EqualizerBand.Peaking(1000f, 1f, 6f),
                EqualizerBand.LowShelf(120f, -4f)));

        [Test]
        public void GraphicEqualizerFullyResets()
        {
            var eq = new GraphicEqualizer();
            eq.SetBandGain(2, 6f);
            eq.SetBandGain(5, -5f);
            AssertResetIsClean(eq);
        }

        [Test]
        public void MultibandCompressorFullyResets()
            => AssertResetIsClean(new MultibandCompressorEffect(250f, 2500f));

        [Test]
        public void ConvolutionReverbFullyResets()
        {
            var ir = new float[512];
            uint s = 0x12345677;
            for (var i = 0; i < ir.Length; i++)
            {
                s ^= s << 13; s ^= s >> 17; s ^= s << 5;
                ir[i] = (s / 2147483648f - 1f) * MathF.Exp(-i / 120f);
            }
            var fx = new ConvolutionReverbEffect();
            fx.SetImpulseResponse(ir);
            AssertResetIsClean(fx);
        }
    }
}
