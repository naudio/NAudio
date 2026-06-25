using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Asio.Tests;

/// <summary>
/// Records from selected input channels and reports per-channel peak RMS over the duration.
/// Useful for verifying which physical channel each <c>InputChannels</c> index maps to.
/// </summary>
/// <remarks>
/// The legacy version had a live in-place dBFS bar UI. That's been dropped in favour of a single
/// peak-RMS summary at the end — same diagnostic value, works identically in CLI and menu modes.
/// </remarks>
internal sealed class AsioShowChannelLevelsTest : IConsoleTest
{
    public string Id => "Asio.ShowChannelLevels";
    public string Description => "Measure per-channel peak RMS over a fixed duration (no WAV saved)";
    public MenuPath? MenuLocation =>
        new("ASIO (AsioDevice — NAudio 3)", "Show per-channel input levels", Group: "Recording", Order: 1);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("driver", typeof(string), Required: true, Help: "installed ASIO driver name",
            ChoiceProvider: AsioDrivers.DriverNames),
        new("inputChannels", typeof(string), Required: true,
            Help: "comma-separated input channel indices (e.g. '0,1')",
            InteractivePrompter: AsioDrivers.PickInputChannels),
        new("duration", typeof(TimeSpan), Required: false, Default: TimeSpan.FromSeconds(5),
            Help: "how long to monitor levels"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var driverName = ctx.Get<string>("driver");
        using var device = AsioDrivers.TryOpen(driverName);
        if (device is null) return TestResult.Fail($"ASIO driver not installed: {driverName}");
        if (device.Capabilities.NbInputChannels == 0)
            return TestResult.Fail("Driver has no input channels");

        if (!AsioDrivers.TryParseChannels(ctx.Get<string>("inputChannels"),
            device.Capabilities.NbInputChannels, out var channels, out var err))
            return TestResult.Fail($"--inputChannels: {err}");

        var duration = ctx.Get<TimeSpan>("duration");

        try
        {
            device.InitRecording(new AsioRecordingOptions
            {
                InputChannels = channels,
                SampleRate = device.CurrentSampleRate,
            });
        }
        catch (Exception ex)
        {
            return TestResult.Fail($"Init failed: {ex.Message}");
        }

        // Per-callback RMS feeds peakRms (per-channel max-of-RMS over the run) and currentRms
        // (most recent value per channel, used by the live meter when interactive).
        var peakRms = new float[channels.Length];
        var currentRms = new float[channels.Length];
        device.AudioCaptured += (_, e) =>
        {
            for (var ch = 0; ch < e.ChannelCount; ch++)
            {
                var span = e.GetChannel(ch);
                double sumSq = 0;
                for (var i = 0; i < span.Length; i++) sumSq += span[i] * span[i];
                var rms = (float)Math.Sqrt(sumSq / Math.Max(1, span.Length));
                currentRms[ch] = rms;
                if (rms > peakRms[ch]) peakRms[ch] = rms;
            }
        };

        AnsiConsole.MarkupLine(ctx.Interactive
            ? $"[green]Monitoring for {duration.TotalSeconds:F1}s[/] [dim](ESC to stop early)[/]\n"
            : $"[green]Monitoring for {duration.TotalSeconds:F1}s[/] [dim](Ctrl+C to stop early)[/]\n");
        device.Start();

        var cancelled = false;
        var deadline = DateTime.UtcNow + duration;

        if (ctx.Interactive)
        {
            // Live meter: in-place bar per channel, refreshed at ~10 Hz. RMS rarely exceeds
            // ~0.25 so amplify the bar by 4× to make low signal visible.
            using var meter = new LiveMeterRenderer(channels.Length);
            while (DateTime.UtcNow < deadline)
            {
                if (Console.KeyAvailable && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape) break;
                if (ctx.Cancellation.WaitHandle.WaitOne(100)) { cancelled = true; break; }
                for (var i = 0; i < channels.Length; i++)
                    meter.Update(i, $"ch{channels[i]}", currentRms[i], scale: 4f);
            }
        }
        else
        {
            cancelled = ctx.Cancellation.WaitHandle.WaitOne(duration);
        }

        try { device.Stop(); } catch { }

        var diagnostics = new Dictionary<string, string>
        {
            ["driver"] = driverName,
            ["channels"] = string.Join(",", channels),
            ["durationMs"] = duration.TotalMilliseconds.ToString("F0"),
        };

        var summary = new Table().AddColumn("Channel").AddColumn("Peak RMS").AddColumn("Peak dBFS");
        for (var i = 0; i < channels.Length; i++)
        {
            var rms = peakRms[i];
            var db = rms > 1e-6f ? 20f * MathF.Log10(rms) : float.NegativeInfinity;
            summary.AddRow(
                channels[i].ToString(),
                rms.ToString("F6"),
                float.IsNegativeInfinity(db) ? "-∞" : $"{db:F1}");
            diagnostics[$"ch{channels[i]}_peakRms"] = rms.ToString("F6");
        }
        AnsiConsole.Write(summary);

        return cancelled
            ? TestResult.Skipped("Cancelled")
            : TestResult.Pass($"Monitored {duration.TotalSeconds:F1}s across {channels.Length} channel(s)", diagnostics);
    }
}
