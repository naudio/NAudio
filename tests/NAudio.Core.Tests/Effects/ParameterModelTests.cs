using System;
using System.Linq;
using NAudio.Effects;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Effects;

[TestFixture]
[Category("UnitTest")]
public class ParameterModelTests
{
    private static WaveFormat Mono => WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);

    private static EffectParameter Param(IParameterized e, string name)
        => e.Parameters.First(p => p.Name == name);

    [Test]
    public void ContinuousParameterRoundTripsToTheProperty()
    {
        var gain = new GainEffect();
        var p = Param(gain, "Gain");

        p.Value = -6f;

        Assert.That(p.Kind, Is.EqualTo(EffectParameterKind.Continuous));
        Assert.That(gain.GainDb, Is.EqualTo(-6f).Within(1e-3f));
        Assert.That(p.Value, Is.EqualTo(-6f).Within(1e-3f));
    }

    [Test]
    public void WritesAreClampedToRange()
    {
        var gain = new GainEffect();
        var p = Param(gain, "Gain"); // -60..24

        p.Value = 999f;
        Assert.That(p.Value, Is.EqualTo(24f).Within(1e-3f));
        p.Value = -999f;
        Assert.That(p.Value, Is.EqualTo(-60f).Within(1e-3f));
    }

    [Test]
    public void ToggleParameterMapsToBool()
    {
        var limiter = new LimiterEffect();
        var tp = Param(limiter, "True Peak");

        Assert.That(tp.Kind, Is.EqualTo(EffectParameterKind.Toggle));
        tp.Value = 0f;
        Assert.That(limiter.TruePeak, Is.False);
        tp.Value = 1f;
        Assert.That(limiter.TruePeak, Is.True);
    }

    [Test]
    public void ChoiceParameterMapsToEnum()
    {
        var sat = new SaturationEffect();
        var curve = Param(sat, "Curve");

        Assert.That(curve.Kind, Is.EqualTo(EffectParameterKind.Choice));
        Assert.That(curve.Choices, Has.Count.EqualTo(4));
        curve.Value = 3; // Hard Clip
        Assert.That(sat.Curve, Is.EqualTo(SaturationCurve.HardClip));
        curve.Value = 0;
        Assert.That(sat.Curve, Is.EqualTo(SaturationCurve.Tanh));
    }

    [Test]
    public void MeterParameterIsReadOnlyAndReflectsLiveValue()
    {
        var comp = new CompressorEffect { ThresholdDb = -24f, Ratio = 6f };
        comp.Configure(Mono);
        var meter = Param(comp, "Gain Reduction");

        Assert.That(meter.Kind, Is.EqualTo(EffectParameterKind.Meter));
        Assert.That(meter.IsReadOnly, Is.True);
        meter.Value = 99f; // ignored

        var buffer = new float[48000];
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = MathF.Sin(i * 0.1f);
        comp.Process(buffer);

        Assert.That(meter.Value, Is.GreaterThan(0f));
        Assert.That(meter.Value, Is.EqualTo(comp.GainReductionDb).Within(1e-4f));
    }

    [Test]
    public void BypassAndMixAreNotInTheParameterList()
    {
        var comp = new CompressorEffect();
        Assert.That(comp.Parameters.Any(p => p.Name is "Bypass" or "Mix"), Is.False);
    }

    [Test]
    public void EveryWiredEffectExposesUsableParameters()
    {
        IParameterized[] effects =
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

        foreach (var effect in effects)
        {
            Assert.That(effect.Parameters, Is.Not.Empty, effect.GetType().Name);
            ((AudioEffect)effect).Configure(Mono);

            // Drive every continuous parameter to its midpoint, then process.
            foreach (var p in effect.Parameters)
                if (p.Kind == EffectParameterKind.Continuous && !p.IsReadOnly)
                    p.Value = (p.Minimum + p.Maximum) * 0.5f;

            var buffer = new float[2048];
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = 0.2f * MathF.Sin(i * 0.05f);
            ((AudioEffect)effect).Process(buffer);

            foreach (var s in buffer)
                Assert.That(float.IsFinite(s), Is.True, effect.GetType().Name);
        }
    }
}
