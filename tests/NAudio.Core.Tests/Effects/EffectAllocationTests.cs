using System;
using NAudio.Effects;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Effects;

/// <summary>
/// Pins the "allocation-free steady state" claim: after warm-up, a representative
/// effect of each mechanism (vectorised gain, dynamics, delay line, feedback
/// reverb, biquad, crossfading EQ) must allocate zero managed bytes per
/// <c>Process</c> on the audio thread.
/// </summary>
[TestFixture]
[Category("UnitTest")]
public class EffectAllocationTests
{
    private static WaveFormat Mono => WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);

    private static AudioEffect[] Representative() => new AudioEffect[]
    {
        new GainEffect(),
        new CompressorEffect(),
        new DelayEffect(),
        new ReverbEffect(),
        new DcBlockerEffect(),
        new Equalizer(EqualizerBand.Peaking(1000f, 1f, 6f))
    };

    [Test]
    public void SteadyStateProcessDoesNotAllocate(
        [ValueSource(nameof(Representative))] AudioEffect effect)
    {
        effect.Configure(Mono);

        var buffer = new float[1024];
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = 0.25f * MathF.Sin(i * 0.05f);

        // Warm up: JIT the Process path and let any one-time lazy buffers
        // (e.g. the base class dry-buffer for Mix < 1) allocate.
        for (var w = 0; w < 64; w++)
            effect.Process(buffer);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var p = 0; p < 512; p++)
            effect.Process(buffer);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.EqualTo(0L),
            $"{effect.GetType().Name} allocated {allocated} bytes across 512 Process calls");
    }
}
