using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Wasapi.Tests;

sealed class WasapiRecordToWavFileTest : IConsoleTest
{
    public string Id => "Wasapi.RecordToWavFile";
    public string Description => "Record from capture endpoint into a WAV file for a fixed duration";
    public MenuPath? MenuLocation => new("WASAPI", "Record to WAV file", Group: "Recorder", Order: 1);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("captureDevice", typeof(string), Required: false, Default: WasapiDevices.DefaultMarker,
            Help: "capture endpoint friendly name (or 'default')",
            ChoiceProvider: WasapiDevices.CaptureDeviceNames),
        new("output", typeof(string), Required: false,
            Help: "output WAV path (auto-named on Desktop if blank)"),
        new("duration", typeof(TimeSpan), Required: false, Default: TimeSpan.FromSeconds(10),
            Help: "recording duration"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var captureName = ctx.Get<string>("captureDevice");
        var captureDevice = WasapiDevices.ResolveCapture(captureName);
        if (captureDevice is null) return TestResult.Fail($"Capture device not found: {captureName}");

        var duration = ctx.Get<TimeSpan>("duration");
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

        AnsiConsole.MarkupLine($"[grey]Device:[/]   {Markup.Escape(captureDevice.FriendlyName)}");
        AnsiConsole.MarkupLine($"[grey]Output:[/]   {Markup.Escape(filePath)}");
        AnsiConsole.MarkupLine($"[grey]Duration:[/] {duration.TotalSeconds:F0}s");
        AnsiConsole.WriteLine();

        using var recorder = new WasapiRecorderBuilder()
            .WithDevice(captureDevice)
            .WithSharedMode()
            .WithEventSync()
            .Build();

        var writer = new WaveFileWriter(filePath, recorder.WaveFormat);
        long pcmBytes = 0;
        recorder.DataAvailable += (buffer, flags) =>
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
            ["captureDevice"] = captureDevice.FriendlyName,
            ["outputPath"] = filePath,
            ["outputBytes"] = outputInfo.Length.ToString(),
            ["pcmBytes"] = pcmBytes.ToString(),
            ["recordedDurationMs"] = recordedDuration.TotalMilliseconds.ToString("F0"),
        };

        AnsiConsole.MarkupLine($"\n[grey]Saved {outputInfo.Length / 1024}KB to {Markup.Escape(filePath)}[/]");
        AnsiConsole.MarkupLine($"[grey]Recorded: {recordedDuration:mm\\:ss\\.f}[/]");

        if (cancelled)
            return TestResult.Skipped($"Cancelled — file kept ({recordedDuration:mm\\:ss\\.f} recorded)");

        return pcmBytes == 0
            ? TestResult.Fail("No audio data recorded (silent device?)", diagnostics)
            : TestResult.Pass($"Recorded {recordedDuration:mm\\:ss\\.f} to {Path.GetFileName(filePath)}", diagnostics);
    }
}
