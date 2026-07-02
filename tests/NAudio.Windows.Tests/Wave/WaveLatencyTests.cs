using System;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Windows.Tests.Wave;

/// <summary>
/// Exercises the pure-managed surface of <see cref="IWaveLatency"/> on the WinMM
/// players that don't touch hardware until <c>Init</c> / <c>StartRecording</c>:
/// the constructors only initialise managed state, so the buffer-config formulas
/// and the pre-Init fallback path can be unit-tested without a real audio device.
/// The timestamp-selection logic that drives <see cref="WaveOut.CurrentLatency"/>
/// during playback is exercised via <see cref="WaveOutLatencyHelper"/>.
/// </summary>
[TestFixture]
[Category("UnitTest")]
public class WaveLatencyTests
{
    private static readonly TimeSpan Tolerance = TimeSpan.FromMilliseconds(1);

    [TestCase(50, 3, 125.0)]
    [TestCase(100, 2, 150.0)]
    [TestCase(10, 4, 35.0)]
    public void WaveOut_AverageLatency_Follows_BufferFormula(int bufferMs, int numBuffers, double expectedMs)
    {
        var waveOut = new WaveOut { BufferMilliseconds = bufferMs, NumberOfBuffers = numBuffers };
        Assert.That(waveOut.AverageLatency,
            Is.EqualTo(TimeSpan.FromMilliseconds(expectedMs)).Within(Tolerance));
    }

    [Test]
    public void WaveOut_CurrentLatency_BeforeInit_Returns_AverageLatency()
    {
        var waveOut = new WaveOut { BufferMilliseconds = 100, NumberOfBuffers = 2 };
        Assert.That(waveOut.CurrentLatency,
            Is.EqualTo(waveOut.AverageLatency).Within(Tolerance));
    }

    [TestCase(50, 3, 125.0)]
    [TestCase(100, 3, 250.0)]
    [TestCase(200, 2, 300.0)]
    public void WaveIn_AverageLatency_Follows_BufferFormula(int bufferMs, int numBuffers, double expectedMs)
    {
        var waveIn = new WaveIn { BufferMilliseconds = bufferMs, NumberOfBuffers = numBuffers };
        Assert.That(waveIn.AverageLatency,
            Is.EqualTo(TimeSpan.FromMilliseconds(expectedMs)).Within(Tolerance));
    }

    [Test]
    public void WaveIn_CurrentLatency_BeforeRecording_Returns_AverageLatency()
    {
        var waveIn = new WaveIn();
        Assert.That(waveIn.CurrentLatency,
            Is.EqualTo(waveIn.AverageLatency).Within(Tolerance));
    }

    [Test]
    public void Players_And_Captures_Implement_IWaveLatency()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new WaveOut(), Is.InstanceOf<IWaveLatency>());
            Assert.That(new WaveIn(), Is.InstanceOf<IWaveLatency>());
        });
    }

    // --- WaveOutLatencyHelper: exercises the timestamp-selection logic that WaveOut and
    // WaveOutWindow both delegate to. Uses fabricated timestamps so the driver-owned
    // WaveOutBuffer isn't needed.

    [Test]
    public void Helper_ReturnsAverage_WhenNoTimestamps()
    {
        var avg = TimeSpan.FromMilliseconds(200);
        var result = WaveOutLatencyHelper.CurrentLatencyFromOldestFilledTimestamp(
            queuedFilledTimestamps: ReadOnlySpan<long>.Empty,
            averageLatency: avg,
            nowTicks: 1_000_000,
            stopwatchFrequency: 10_000_000);
        Assert.That(result, Is.EqualTo(avg));
    }

    [Test]
    public void Helper_ReturnsAverage_WhenAllTimestampsAreMinValue()
    {
        var avg = TimeSpan.FromMilliseconds(200);
        Span<long> stamps = stackalloc long[3] { long.MinValue, long.MinValue, long.MinValue };
        var result = WaveOutLatencyHelper.CurrentLatencyFromOldestFilledTimestamp(
            stamps, avg, nowTicks: 1_000_000, stopwatchFrequency: 10_000_000);
        Assert.That(result, Is.EqualTo(avg));
    }

    [Test]
    public void Helper_PicksOldestTimestamp_IgnoringMinValue()
    {
        // Frequency = 1000 ticks/s → 1 tick = 1 ms
        const long freq = 1_000;
        const long now = 1_000;
        // Newest, oldest queued, and one unfilled slot
        Span<long> stamps = stackalloc long[3] { 900, 700, long.MinValue };
        var result = WaveOutLatencyHelper.CurrentLatencyFromOldestFilledTimestamp(
            stamps, averageLatency: TimeSpan.FromMilliseconds(500), nowTicks: now, stopwatchFrequency: freq);
        // now - 700 = 300 ticks = 300 ms
        Assert.That(result,
            Is.EqualTo(TimeSpan.FromMilliseconds(300)).Within(Tolerance));
    }

    [Test]
    public void Helper_ReturnsZero_WhenNowEqualsOldest()
    {
        Span<long> stamps = stackalloc long[1] { 1000 };
        var result = WaveOutLatencyHelper.CurrentLatencyFromOldestFilledTimestamp(
            stamps, averageLatency: TimeSpan.FromMilliseconds(100), nowTicks: 1000, stopwatchFrequency: 1000);
        Assert.That(result, Is.EqualTo(TimeSpan.Zero));
    }
}
