using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Asio.Tests;

internal sealed class AsioRecordToWavTest : IConsoleTest
{
    public string Id => "Asio.RecordToWav";
    public string Description => "Record from ASIO input channels into a WAV file";
    public MenuPath? MenuLocation => new("ASIO (AsioDevice — NAudio 3)", "Record to WAV file", Group: "Recording", Order: 0);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("driver", typeof(string), Required: true, Help: "installed ASIO driver name",
            ChoiceProvider: AsioDrivers.DriverNames),
        new("inputChannels", typeof(string), Required: true,
            Help: "comma-separated input channel indices (e.g. '0,1')",
            InteractivePrompter: AsioDrivers.PickInputChannels),
        new("duration", typeof(TimeSpan), Required: false, Default: TimeSpan.FromSeconds(10),
            Help: "recording duration"),
        new("output", typeof(string), Required: false,
            Help: "output WAV path (auto-named on Desktop if blank)"),
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
        ctx.TryGet<string>("output", out var filePath);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"NAudio_Asio_{DateTime.Now:yyyyMMdd_HHmmss}.wav");
        }
        else
        {
            var parent = Path.GetDirectoryName(Path.GetFullPath(filePath));
            if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
        }

        var sampleRate = device.CurrentSampleRate;
        AnsiConsole.MarkupLine($"[grey]Sample rate:[/] {sampleRate} Hz");
        AnsiConsole.MarkupLine($"[grey]Channels:[/]    [{string.Join(",", channels)}]");
        AnsiConsole.MarkupLine($"[grey]Output:[/]      {Markup.Escape(filePath)}");
        AnsiConsole.MarkupLine($"[grey]Duration:[/]    {duration.TotalSeconds:F0}s");

        try
        {
            device.InitRecording(new AsioRecordingOptions
            {
                InputChannels = channels,
                SampleRate = sampleRate,
            });
        }
        catch (Exception ex)
        {
            return TestResult.Fail($"Init failed: {ex.Message}");
        }

        var writer = new WaveFileWriter(filePath, WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels.Length));
        long framesWritten = 0;

        device.AudioCaptured += (_, e) =>
        {
            // Interleave channel spans into the WAV. Callback fires on ASIO thread; we're the only writer.
            var localWriter = writer;
            if (localWriter == null) return;
            for (var frame = 0; frame < e.Frames; frame++)
            {
                for (var ch = 0; ch < e.ChannelCount; ch++)
                    localWriter.WriteSample(e.GetChannel(ch)[frame]);
            }
            Interlocked.Add(ref framesWritten, e.Frames);
        };

        var completed = new ManualResetEventSlim();
        Exception? stopException = null;
        device.Stopped += (_, e) => { stopException = e.Exception; completed.Set(); };

        AnsiConsole.MarkupLine(ctx.Interactive
            ? "\n[green]Recording[/] [dim](ESC to stop early)[/]\n"
            : "\n[green]Recording[/] [dim](Ctrl+C to stop early)[/]\n");
        var start = DateTime.UtcNow;
        device.Start();
        var cancelled = false;
        while (!completed.IsSet && DateTime.UtcNow - start < duration)
        {
            if (ctx.Interactive && Console.KeyAvailable
                && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape) break;
            if (ctx.Cancellation.WaitHandle.WaitOne(100)) { cancelled = true; break; }
        }
        if (!completed.IsSet)
        {
            try { device.Stop(); } catch { }
            completed.Wait(TimeSpan.FromSeconds(2));
        }

        writer.Dispose();
        var outputInfo = new FileInfo(filePath);

        AnsiConsole.MarkupLine($"[grey]Saved:[/] {Markup.Escape(filePath)} ({outputInfo.Length / 1024} KB)");

        var diagnostics = new Dictionary<string, string>
        {
            ["driver"] = driverName,
            ["outputPath"] = filePath,
            ["outputBytes"] = outputInfo.Length.ToString(),
            ["framesWritten"] = framesWritten.ToString(),
            ["sampleRate"] = sampleRate.ToString(),
            ["channels"] = string.Join(",", channels),
        };

        if (stopException is not null)
            return TestResult.Fail($"Stopped with error: {stopException.Message}", diagnostics);
        if (cancelled)
            return TestResult.Skipped($"Cancelled — file kept ({framesWritten} frames)");
        return framesWritten == 0
            ? TestResult.Fail("No frames captured (driver not producing audio?)", diagnostics)
            : TestResult.Pass($"Recorded {framesWritten} frames to {Path.GetFileName(filePath)}", diagnostics);
    }
}
