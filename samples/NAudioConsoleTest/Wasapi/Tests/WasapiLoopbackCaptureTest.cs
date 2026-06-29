using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudioConsoleTest.Shared;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Wasapi.Tests;

/// <summary>
/// System-wide (whole-device) WASAPI loopback capture via <see cref="WasapiRecorderBuilder.WithLoopbackCapture"/>:
/// records everything a render endpoint is playing into a WAV file. Unlike
/// <see cref="WasapiProcessLoopbackTest"/> this captures the entire device mix, not a single
/// process. With <c>playSine</c> enabled it plays a tone to the same device so there is
/// guaranteed audio to capture (WASAPI loopback delivers nothing while the device is idle).
/// </summary>
internal sealed class WasapiLoopbackCaptureTest : IConsoleTest
{
    public string Id => "Wasapi.LoopbackCapture";
    public string Description => "Capture everything a render device is playing (system-wide WASAPI loopback)";
    public MenuPath? MenuLocation => new("WASAPI", "System loopback capture", Group: "Recorder", Order: 2);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("renderDevice", typeof(string), Required: false, Default: WasapiDevices.DefaultMarker,
            Help: "render endpoint friendly name to capture (or 'default')",
            ChoiceProvider: WasapiDevices.RenderDeviceNames),
        new("playSine", typeof(bool), Required: false, Default: false,
            Help: "also play a sine wave to the device so there is audio to capture"),
        new("duration", typeof(TimeSpan), Required: false, Default: TimeSpan.FromSeconds(10),
            Help: "capture duration"),
        new("output", typeof(string), Required: false,
            Help: "output WAV path (auto-named on Desktop if blank)"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var deviceName = ctx.Get<string>("renderDevice");
        var device = WasapiDevices.ResolveRender(deviceName);
        if (device is null) return TestResult.Fail($"Render device not found: {deviceName}");

        var playSine = ctx.Get<bool>("playSine");
        var duration = ctx.Get<TimeSpan>("duration");
        ctx.TryGet<string>("output", out var filePath);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"NAudio_Loopback_{DateTime.Now:yyyyMMdd_HHmmss}.wav");
        }
        else
        {
            var parent = Path.GetDirectoryName(Path.GetFullPath(filePath));
            if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
        }

        AnsiConsole.MarkupLine($"[grey]Device:[/]   {Markup.Escape(device.FriendlyName)} [dim](loopback)[/]");
        AnsiConsole.MarkupLine($"[grey]Output:[/]   {Markup.Escape(filePath)}");
        AnsiConsole.MarkupLine($"[grey]Duration:[/] {duration.TotalSeconds:F0}s");
        AnsiConsole.MarkupLine($"[grey]PlaySine:[/] {playSine}");
        AnsiConsole.WriteLine();

        using var recorder = new WasapiRecorderBuilder()
            .WithDevice(device)
            .WithLoopbackCapture()
            .WithSharedMode()
            .WithEventSync()
            .Build();

        AnsiConsole.MarkupLine($"[grey]Format:[/]   {recorder.WaveFormat}");

        var writer = new WaveFileWriter(filePath, recorder.WaveFormat);
        long pcmBytes = 0;
        long silentPackets = 0;
        recorder.DataAvailable += (buffer, flags, devicePosition, qpcPosition) =>
        {
            if ((flags & AudioClientBufferFlags.Silent) != 0)
            {
                Interlocked.Increment(ref silentPackets);
                return;
            }
            if (!buffer.IsEmpty)
            {
                var arr = buffer.ToArray();
                // ReSharper disable once AccessToDisposedClosure
                writer.Write(arr, 0, arr.Length);
                Interlocked.Add(ref pcmBytes, arr.Length);
            }
        };

        AnsiConsole.MarkupLine(
            $"\n[bold red]Capturing for {duration.TotalSeconds:F0}s...[/] " +
            $"[dim]({(ctx.Interactive ? "ESC stops early" : "Ctrl+C stops early")})[/]");

        recorder.StartRecording();
        using var sinePlayer = playSine ? StartSinePlayer(device) : null;

        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < duration)
        {
            if (ctx.Interactive && Console.KeyAvailable
                && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape) break;
            if (ctx.Cancellation.WaitHandle.WaitOne(100)) break;
        }

        recorder.StopRecording();
        sinePlayer?.Stop();

        var recordedDuration = writer.TotalTime;
        writer.Dispose();

        var cancelled = ctx.Cancellation.IsCancellationRequested;
        var outputInfo = new FileInfo(filePath);

        var diagnostics = new Dictionary<string, string>
        {
            ["renderDevice"] = device.FriendlyName,
            ["outputPath"] = filePath,
            ["outputBytes"] = outputInfo.Length.ToString(),
            ["pcmBytes"] = pcmBytes.ToString(),
            ["silentPackets"] = silentPackets.ToString(),
            ["recordedDurationMs"] = recordedDuration.TotalMilliseconds.ToString("F0"),
        };

        AnsiConsole.MarkupLine($"\n[grey]Saved {outputInfo.Length / 1024}KB to {Markup.Escape(filePath)}[/]");
        AnsiConsole.MarkupLine($"[grey]Recorded: {recordedDuration:mm\\:ss\\.f} ({silentPackets} silent packets)[/]");

        if (cancelled)
            return TestResult.Skipped($"Cancelled — file kept ({recordedDuration:mm\\:ss\\.f} recorded)", diagnostics);

        // Capturing succeeded even if nothing was playing — WASAPI loopback simply yields no
        // (or silent) data when the device is idle, so zero bytes is not a failure here.
        return pcmBytes > 0
            ? TestResult.Pass($"Captured {pcmBytes / 1024}KB ({recordedDuration:mm\\:ss\\.f}) to {Path.GetFileName(filePath)}", diagnostics)
            : TestResult.Pass("Loopback capture ran but no audio was playing on the device", diagnostics);
    }

    private static IWavePlayer? StartSinePlayer(MMDevice device)
    {
        WasapiVolumeSafety.CapAt(device);
        var player = new WasapiPlayerBuilder()
            .WithDevice(device)
            .WithSharedMode()
            .WithEventSync()
            .Build();
        player.Init(new SineWaveSource(440f, 0.25f));
        player.Play();
        AnsiConsole.MarkupLine($"[green]✓[/] Playing 440Hz sine on [grey]{Markup.Escape(device.FriendlyName)}[/]");
        return player;
    }
}
