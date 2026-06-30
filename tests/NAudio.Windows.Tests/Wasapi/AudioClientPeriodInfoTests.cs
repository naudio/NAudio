using NAudio.CoreAudioApi;
using NUnit.Framework;

namespace NAudio.Windows.Tests.Wasapi;

/// <summary>
/// Unit tests for <see cref="AudioClientPeriodInfo.ChooseLowestLatencyPeriod"/>, the pure period-selection
/// math shared by WasapiPlayer and WasapiRecorder for IAudioClient3 low-latency mode. No audio hardware
/// required, so these are not marked IntegrationTest.
/// </summary>
[TestFixture]
public class AudioClientPeriodInfoTests
{
    [Test]
    public void MinPeriodAlreadyMultipleOfFundamental_ReturnsMinUnchanged()
    {
        // 144 is an exact multiple of 48, so it is already a valid period.
        var info = new AudioClientPeriodInfo(defaultPeriod: 480, fundamentalPeriod: 48, minPeriod: 144, maxPeriod: 960);
        Assert.That(info.ChooseLowestLatencyPeriod(), Is.EqualTo(144u));
    }

    [Test]
    public void MinPeriodNotMultipleOfFundamental_RoundsUpToNextMultiple()
    {
        // 130 is not a multiple of 48; the next valid period is 144.
        var info = new AudioClientPeriodInfo(defaultPeriod: 480, fundamentalPeriod: 48, minPeriod: 130, maxPeriod: 960);
        Assert.That(info.ChooseLowestLatencyPeriod(), Is.EqualTo(144u));
    }

    [Test]
    public void RoundedUpPeriodExceedsMax_ClampsToMax()
    {
        // Rounding 130 up to a multiple of 48 gives 144, which is above the 140 maximum, so clamp.
        var info = new AudioClientPeriodInfo(defaultPeriod: 140, fundamentalPeriod: 48, minPeriod: 130, maxPeriod: 140);
        Assert.That(info.ChooseLowestLatencyPeriod(), Is.EqualTo(140u));
    }

    [Test]
    public void ZeroFundamental_ReturnsMinUnchanged()
    {
        // A zero fundamental period means no multiple constraint — use the minimum as-is.
        var info = new AudioClientPeriodInfo(defaultPeriod: 480, fundamentalPeriod: 0, minPeriod: 161, maxPeriod: 960);
        Assert.That(info.ChooseLowestLatencyPeriod(), Is.EqualTo(161u));
    }
}
