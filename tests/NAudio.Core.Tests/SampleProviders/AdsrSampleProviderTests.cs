using System;
using NUnit.Framework;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace NAudio.Core.Tests.SampleProviders;

[TestFixture]
public class AdsrSampleProviderTests
{
    private const int SampleRate = 44100;

    // A source that always returns its configured constant value, so the value seen after
    // Read equals the envelope gain applied to that frame.
    private sealed class ConstantSampleProvider : ISampleProvider
    {
        private readonly float value;

        public ConstantSampleProvider(int sampleRate, int channels, float value = 1.0f)
        {
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            this.value = value;
        }

        public WaveFormat WaveFormat { get; }

        public int Read(Span<float> buffer)
        {
            buffer.Fill(value);
            return buffer.Length;
        }
    }

    // Pumps the provider one frame at a time until it goes idle (Read returns 0), returning
    // the number of frames produced. Guarded against runaway loops.
    private static int PumpToIdle(AdsrSampleProvider adsr, int channels)
    {
        var frame = new float[channels];
        int frames = 0;
        while (adsr.Read(frame) > 0 && frames < SampleRate * 10)
        {
            frames++;
        }
        return frames;
    }

    [Test]
    public void DefaultsHoldAtFullLevelThenReleaseFadesToZero()
    {
        var adsr = new AdsrSampleProvider(new ConstantSampleProvider(SampleRate, 1));
        var frame = new float[1];

        // pump past the 0.01s attack to reach the sustain plateau
        for (int i = 0; i < 2000; i++) adsr.Read(frame);

        Assert.That(adsr.State, Is.EqualTo(EnvelopeGenerator.EnvelopeState.Sustain));
        Assert.That(frame[0], Is.EqualTo(1.0f).Within(1e-3f));

        adsr.Stop();
        PumpToIdle(adsr, 1);

        Assert.That(adsr.State, Is.EqualTo(EnvelopeGenerator.EnvelopeState.Idle));
        Assert.That(adsr.Read(frame), Is.EqualTo(0));
    }

    [Test]
    public void DecayFallsToSustainLevel()
    {
        var adsr = new AdsrSampleProvider(new ConstantSampleProvider(SampleRate, 1))
        {
            AttackSeconds = 0.001f,
            DecaySeconds = 0.005f,
            SustainLevel = 0.5f,
            ReleaseSeconds = 0.1f
        };
        var frame = new float[1];

        // pump well past attack + decay (~0.006s ≈ 265 samples)
        for (int i = 0; i < 3000; i++) adsr.Read(frame);

        Assert.That(adsr.State, Is.EqualTo(EnvelopeGenerator.EnvelopeState.Sustain));
        Assert.That(frame[0], Is.EqualTo(0.5f).Within(1e-2f));
    }

    [Test]
    public void EnvelopeCompletedRaisedExactlyOnceAfterRelease()
    {
        var adsr = new AdsrSampleProvider(new ConstantSampleProvider(SampleRate, 1))
        {
            AttackSeconds = 0.001f,
            ReleaseSeconds = 0.005f
        };
        int completedCount = 0;
        adsr.EnvelopeCompleted += (_, _) => completedCount++;
        var frame = new float[1];

        for (int i = 0; i < 500; i++) adsr.Read(frame); // reach sustain
        adsr.Stop();
        PumpToIdle(adsr, 1);

        // extra idle reads must not re-raise the event
        adsr.Read(frame);
        adsr.Read(frame);

        Assert.That(completedCount, Is.EqualTo(1));
    }

    [Test]
    public void AppliesSameGainToAllChannelsOfAStereoSource()
    {
        var adsr = new AdsrSampleProvider(new ConstantSampleProvider(SampleRate, 2))
        {
            AttackSeconds = 0.01f
        };
        var frame = new float[2];

        // mid-attack: gain is between 0 and 1 but identical across the frame
        for (int i = 0; i < 100; i++) adsr.Read(frame);

        Assert.That(frame[0], Is.GreaterThan(0f));
        Assert.That(frame[1], Is.EqualTo(frame[0]));
    }

    [Test]
    public void StartRetriggersAfterCompletion()
    {
        var adsr = new AdsrSampleProvider(new ConstantSampleProvider(SampleRate, 1))
        {
            AttackSeconds = 0.001f,
            ReleaseSeconds = 0.001f
        };
        var frame = new float[1];

        for (int i = 0; i < 200; i++) adsr.Read(frame);
        adsr.Stop();
        PumpToIdle(adsr, 1);

        Assert.That(adsr.State, Is.EqualTo(EnvelopeGenerator.EnvelopeState.Idle));
        Assert.That(adsr.Read(frame), Is.EqualTo(0));

        adsr.Start();

        Assert.That(adsr.State, Is.Not.EqualTo(EnvelopeGenerator.EnvelopeState.Idle));
        Assert.That(adsr.Read(frame), Is.EqualTo(1));
    }

    [Test]
    public void DoesNotProduceSamplesUntilStartedWhenGateInitiallyClosed()
    {
        var adsr = new AdsrSampleProvider(new ConstantSampleProvider(SampleRate, 1), startGateOpen: false);

        Assert.That(adsr.State, Is.EqualTo(EnvelopeGenerator.EnvelopeState.Idle));
        Assert.That(adsr.Read(new float[10]), Is.EqualTo(0));

        adsr.Start();

        Assert.That(adsr.Read(new float[1]), Is.EqualTo(1));
    }

    [Test]
    public void SupportsStereoWithoutThrowing()
    {
        Assert.DoesNotThrow(() => new AdsrSampleProvider(new ConstantSampleProvider(SampleRate, 2)));
    }
}
