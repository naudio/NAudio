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

    private const int ProcessCallsPerWindow = 512;

    /// <summary>
    /// How many consecutive measurement windows an effect may use to produce an allocation-free one.
    /// </summary>
    private const int MeasurementWindows = 3;

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

        // Measure more than once, stopping at the first allocation-free window. Warm-up cannot
        // fully guarantee that nothing one-off lands on this thread afterwards - a tiering
        // transition or a lazy runtime init has no call count we can wait out - and this test has
        // failed in CI on a single one-off of ~1.4KB, which is not a per-call cost. What the claim
        // actually needs is that no window allocates, and a genuine per-call allocation dirties
        // every window, so retrying keeps the guarantee strict while dropping that false failure.
        var allocated = long.MaxValue;
        var windowsUsed = 0;
        while (windowsUsed < MeasurementWindows && allocated != 0)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var p = 0; p < ProcessCallsPerWindow; p++)
                effect.Process(buffer);
            allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            windowsUsed++;
        }

        if (allocated == 0 && windowsUsed > 1)
        {
            // Not a failure, but worth surfacing: if this line starts appearing routinely, the
            // cause is likely a real lazy allocation rather than one-off runtime noise.
            TestContext.WriteLine(
                $"{effect.GetType().Name}: allocation-free on window {windowsUsed} of {MeasurementWindows}, " +
                "after an earlier window allocated.");
        }

        Assert.That(allocated, Is.EqualTo(0L),
            $"{effect.GetType().Name} allocated {allocated} bytes across {ProcessCallsPerWindow} Process " +
            $"calls in every one of {MeasurementWindows} consecutive windows");
    }
}
