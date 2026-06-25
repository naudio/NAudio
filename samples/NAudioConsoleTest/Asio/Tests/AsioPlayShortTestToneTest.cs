using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudioConsoleTest.Shared;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Asio.Tests;

sealed class AsioPlayShortTestToneTest : IConsoleTest
{
    public string Id => "Asio.PlayShortTestTone";
    public string Description => "Play a low-amplitude sine tone through selected ASIO output channels";
    public MenuPath? MenuLocation =>
        new("ASIO (AsioDevice — NAudio 3)", "Play short test tone (quiet 440Hz, 2s)",
            Group: "Playback", Order: 1);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("driver", typeof(string), Required: true, Help: "installed ASIO driver name",
            ChoiceProvider: AsioDrivers.DriverNames),
        new("outputChannels", typeof(string), Required: true,
            Help: "comma-separated output channel indices (e.g. '0,1')",
            InteractivePrompter: AsioDrivers.PickOutputChannels),
        new("frequency", typeof(float), Required: false, Default: 440f, Help: "tone frequency in Hz"),
        new("amplitude", typeof(float), Required: false, Default: 0.05f,
            Help: "tone amplitude — keep low, ASIO bypasses Windows mixer"),
        new("duration", typeof(TimeSpan), Required: false, Default: TimeSpan.FromSeconds(2)),
    ];

    public TestResult Run(TestContext ctx)
    {
        var driverName = ctx.Get<string>("driver");
        using var device = AsioDrivers.TryOpen(driverName);
        if (device is null) return TestResult.Fail($"ASIO driver not installed: {driverName}");

        if (device.Capabilities.NbOutputChannels == 0)
            return TestResult.Fail("Driver has no output channels");

        if (!AsioDrivers.TryParseChannels(ctx.Get<string>("outputChannels"),
            device.Capabilities.NbOutputChannels, out var channels, out var err))
            return TestResult.Fail($"--outputChannels: {err}");

        var frequency = ctx.Get<float>("frequency");
        var amplitude = ctx.Get<float>("amplitude");
        var duration = ctx.Get<TimeSpan>("duration");

        var sampleRate = device.CurrentSampleRate;
        var tone = new SineWaveSource(frequency, amplitude, sampleRate, channels.Length);
        var limited = new OffsetSampleProvider(tone) { Take = duration };

        try
        {
            device.InitPlayback(new AsioPlaybackOptions
            {
                Source = limited.ToWaveProvider(),
                OutputChannels = channels,
                AutoStopOnEndOfStream = true,
            });
        }
        catch (Exception ex)
        {
            return TestResult.Fail($"Init failed: {ex.Message}");
        }

        return AsioPlayAudioFileTest.RunDevicePlayback(device, ctx,
            $"{frequency:F0}Hz sine ({duration.TotalSeconds:F1}s, amp {amplitude:F3}) → channels [{string.Join(",", channels)}]",
            // tone runs for `duration`; add a small grace for the device to actually stop.
            duration + TimeSpan.FromSeconds(2),
            new Dictionary<string, string>
            {
                ["driver"] = driverName,
                ["outputChannels"] = string.Join(",", channels),
                ["frequencyHz"] = frequency.ToString("F0"),
                ["amplitude"] = amplitude.ToString("F3"),
                ["sampleRate"] = sampleRate.ToString(),
            });
    }
}
