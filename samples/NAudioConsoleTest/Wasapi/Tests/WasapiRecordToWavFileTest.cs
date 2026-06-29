using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Wasapi.Tests;

internal sealed class WasapiRecordToWavFileTest : IConsoleTest
{
    public string Id => "Wasapi.RecordToWavFile";
    public string Description => "Record from capture endpoint into a WAV file for a fixed duration";
    public MenuPath? MenuLocation => new("WASAPI", "Record to WAV file", Group: "Recorder", Order: 1);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("captureDevice", typeof(string), Required: false, Default: WasapiDevices.DefaultMarker,
            Help: "capture endpoint friendly name, 'default', or auto stream routing",
            ChoiceProvider: WasapiDevices.CaptureDeviceNames),
        new("output", typeof(string), Required: false,
            Help: "output WAV path (auto-named on Desktop if blank)"),
        new("duration", typeof(TimeSpan), Required: false, Default: TimeSpan.FromSeconds(10),
            Help: "recording duration"),
        new("lowLatency", typeof(bool), Required: false, Default: false,
            Help: "request IAudioClient3 low-latency shared capture"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var captureName = ctx.Get<string>("captureDevice");
        var useRouting = WasapiDevices.IsRoutingMarker(captureName);

        MMDevice? captureDevice = null;
        if (!useRouting)
        {
            captureDevice = WasapiDevices.ResolveCapture(captureName);
            if (captureDevice is null) return TestResult.Fail($"Capture device not found: {captureName}");
        }
        var deviceDisplay = captureDevice?.FriendlyName ?? "default device (auto stream routing)";

        var duration = ctx.Get<TimeSpan>("duration");
        var lowLatency = ctx.Get<bool>("lowLatency");
        // Automatic stream routing is standard shared mode only; low latency is not supported with it.
        if (useRouting && lowLatency)
        {
            AnsiConsole.MarkupLine("[yellow]Stream routing does not support low latency — ignoring.[/]");
            lowLatency = false;
        }
        ctx.TryGet<string>("output", out var filePath);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"NAudio_Recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav");
        }
        else
        {
            var parent = Path.GetDirectoryName(Path.GetFullPath(filePath));
            if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
        }

        AnsiConsole.MarkupLine($"[grey]Device:[/]   {Markup.Escape(deviceDisplay)}");
        AnsiConsole.MarkupLine($"[grey]Output:[/]   {Markup.Escape(filePath)}");
        AnsiConsole.MarkupLine($"[grey]Duration:[/] {duration.TotalSeconds:F0}s");
        AnsiConsole.WriteLine();

        WasapiRecorder recorder;
        if (useRouting)
        {
            recorder = new WasapiRecorderBuilder()
                .WithDefaultDeviceStreamRouting()
                .WithEventSync()
                .BuildAsync().GetAwaiter().GetResult();
        }
        else
        {
            var builder = new WasapiRecorderBuilder()
                .WithDevice(captureDevice!)
                .WithSharedMode()
                .WithEventSync();
            if (lowLatency) builder.WithLowLatency();
            recorder = builder.Build();
        }
        using var _recorder = recorder;

        if (lowLatency)
        {
            // LowLatencyActive is only known after the stream is initialized in StartRecording, so this
            // reports the request here; the actual outcome is captured in the diagnostics below.
            AnsiConsole.MarkupLine("[grey]LowLatency:[/] requested");
        }

        var writer = new WaveFileWriter(filePath, recorder.WaveFormat);
        long pcmBytes = 0;
        recorder.DataAvailable += (buffer, flags, devicePosition, qpcPosition) =>
        {
            if ((flags & AudioClientBufferFlags.Silent) == 0)
            {
                var arr = buffer.ToArray();
                writer.Write(arr, 0, arr.Length);
                Interlocked.Add(ref pcmBytes, arr.Length);
            }
        };

        AnsiConsole.MarkupLine(
            $"[bold red]Recording for {duration.TotalSeconds:F0}s...[/] " +
            $"[dim]({(ctx.Interactive ? "ESC stops early" : "Ctrl+C stops early")})[/]");
        recorder.StartRecording();
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < duration)
        {
            if (ctx.Interactive && Console.KeyAvailable
                && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape) break;
            if (ctx.Cancellation.WaitHandle.WaitOne(100)) break;
        }
        recorder.StopRecording();

        var recordedDuration = writer.TotalTime;
        writer.Dispose();

        var cancelled = ctx.Cancellation.IsCancellationRequested;
        var outputInfo = new FileInfo(filePath);

        var diagnostics = new Dictionary<string, string>
        {
            ["captureDevice"] = deviceDisplay,
            ["outputPath"] = filePath,
            ["outputBytes"] = outputInfo.Length.ToString(),
            ["pcmBytes"] = pcmBytes.ToString(),
            ["recordedDurationMs"] = recordedDuration.TotalMilliseconds.ToString("F0"),
            ["lowLatencyRequested"] = lowLatency.ToString(),
            ["lowLatencyActive"] = recorder.LowLatencyActive.ToString(),
            ["latencyMs"] = recorder.LatencyMilliseconds.ToString(),
        };
        if (recorder.LowLatencyUnavailableReason != null)
            diagnostics["lowLatencyUnavailableReason"] = recorder.LowLatencyUnavailableReason;

        AnsiConsole.MarkupLine($"\n[grey]Saved {outputInfo.Length / 1024}KB to {Markup.Escape(filePath)}[/]");
        AnsiConsole.MarkupLine($"[grey]Recorded: {recordedDuration:mm\\:ss\\.f}[/]");

        if (cancelled)
            return TestResult.Skipped($"Cancelled — file kept ({recordedDuration:mm\\:ss\\.f} recorded)");

        return pcmBytes == 0
            ? TestResult.Fail("No audio data recorded (silent device?)", diagnostics)
            : TestResult.Pass($"Recorded {recordedDuration:mm\\:ss\\.f} to {Path.GetFileName(filePath)}", diagnostics);
    }
}
