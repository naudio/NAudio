using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Asio.Tests;

/// <summary>
/// Plays an audio file through chosen output channels of an ASIO driver. Source is attenuated
/// to a safe level — ASIO bypasses the Windows mixer so the driver's panel volume is the only
/// other attenuation.
/// </summary>
sealed class AsioPlayAudioFileTest : IConsoleTest
{
    private const float SafetyVolume = 0.25f;

    public string Id => "Asio.PlayAudioFile";
    public string Description => "Play an audio file through ASIO output channels";
    public MenuPath? MenuLocation => new("ASIO (AsioDevice — NAudio 3)", "Play audio file", Group: "Playback", Order: 0);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("driver", typeof(string), Required: true, Help: "installed ASIO driver name",
            ChoiceProvider: AsioDrivers.DriverNames),
        new("input", typeof(string), Required: true, Help: "audio file path"),
        new("outputChannels", typeof(string), Required: true,
            Help: "comma-separated output channel indices (e.g. '0,1') — count must match source",
            InteractivePrompter: AsioDrivers.PickOutputChannels),
        new("maxDuration", typeof(TimeSpan), Required: false, Default: TimeSpan.FromMinutes(5),
            Help: "playback cap (interactive mode uses ESC instead)", CliOnly: true),
    ];

    public TestResult Run(TestContext ctx)
    {
        var driverName = ctx.Get<string>("driver");
        var inputPath = ctx.Get<string>("input");
        if (!File.Exists(inputPath)) return TestResult.Fail($"Input not found: {inputPath}");

        using var device = AsioDrivers.TryOpen(driverName);
        if (device is null) return TestResult.Fail($"ASIO driver not installed: {driverName}");

        AnsiConsole.MarkupLine($"[yellow]⚠ ASIO bypasses the Windows mixer. " +
                               $"Source attenuated to {SafetyVolume:P0}.[/]");

        using var reader = new MediaFoundationReader(inputPath);
        var sourceChannels = reader.WaveFormat.Channels;

        if (device.Capabilities.NbOutputChannels < sourceChannels)
            return TestResult.Fail(
                $"Driver has {device.Capabilities.NbOutputChannels} outputs but the file needs {sourceChannels}");

        if (!AsioDrivers.TryParseChannels(ctx.Get<string>("outputChannels"),
            device.Capabilities.NbOutputChannels, out var channels, out var err))
            return TestResult.Fail($"--outputChannels: {err}");

        if (channels.Length != sourceChannels)
            return TestResult.Fail($"Need exactly {sourceChannels} output channel(s), got {channels.Length}");

        if (!device.IsSampleRateSupported(reader.WaveFormat.SampleRate))
            return TestResult.Fail($"Driver does not support {reader.WaveFormat.SampleRate} Hz (file's native rate)");

        var safeSource = new VolumeSampleProvider(reader.ToSampleProvider()) { Volume = SafetyVolume };

        try
        {
            device.InitPlayback(new AsioPlaybackOptions
            {
                Source = safeSource.ToWaveProvider(),
                OutputChannels = channels,
                AutoStopOnEndOfStream = true,
            });
        }
        catch (Exception ex)
        {
            return TestResult.Fail($"Init failed: {ex.Message}");
        }

        return RunDevicePlayback(device, ctx,
            $"{Path.GetFileName(inputPath)} → channels [{string.Join(",", channels)}]",
            ctx.Get<TimeSpan>("maxDuration"),
            new Dictionary<string, string>
            {
                ["driver"] = driverName,
                ["input"] = inputPath,
                ["outputChannels"] = string.Join(",", channels),
                ["sampleRate"] = device.CurrentSampleRate.ToString(),
                ["sourceDurationMs"] = reader.TotalTime.TotalMilliseconds.ToString("F0"),
            });
    }

    internal static TestResult RunDevicePlayback(AsioDevice device, TestContext ctx,
        string description, TimeSpan maxDuration, Dictionary<string, string> diagnostics)
    {
        var completed = new ManualResetEventSlim();
        Exception? stopException = null;
        device.Stopped += (_, e) => { stopException = e.Exception; completed.Set(); };

        AnsiConsole.MarkupLine($"[green]Playing[/] {Markup.Escape(description)}");
        AnsiConsole.MarkupLine($"[grey]Buffer:[/] {device.FramesPerBuffer} frames, " +
                               $"output latency {device.OutputLatencySamples} frames");
        AnsiConsole.MarkupLine(ctx.Interactive ? "[dim]ESC to stop early[/]\n" : "[dim]Ctrl+C to stop early[/]\n");

        var start = DateTime.UtcNow;
        device.Start();
        var cancelled = false;
        while (!completed.IsSet)
        {
            if (ctx.Interactive && Console.KeyAvailable
                && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape) break;
            if (ctx.Cancellation.WaitHandle.WaitOne(100)) { cancelled = true; break; }
            // maxDuration is a CLI-only cap; interactive sessions stop via ESC.
            if (!ctx.Interactive && DateTime.UtcNow - start >= maxDuration) break;
        }
        if (!completed.IsSet)
        {
            try { device.Stop(); } catch { }
            completed.Wait(TimeSpan.FromSeconds(2));
        }

        var elapsed = DateTime.UtcNow - start;
        diagnostics["elapsedMs"] = elapsed.TotalMilliseconds.ToString("F0");
        if (stopException is not null)
            diagnostics["stopException"] = $"{stopException.GetType().Name}: {stopException.Message}";

        if (stopException is not null)
            return TestResult.Fail($"Stopped with error: {stopException.Message}", diagnostics);
        if (cancelled)
            return TestResult.Skipped($"Cancelled after {elapsed.TotalSeconds:F1}s");
        return TestResult.Pass($"Played {elapsed.TotalSeconds:F1}s", diagnostics);
    }
}
