using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.WinMM.Tests;

/// <summary>
/// Records from a <see cref="WaveIn"/> device into a WAV file. Records for <c>duration</c>
/// or until cancelled — whichever comes first.
/// </summary>
sealed class WinMmRecordToFileTest : IConsoleTest
{
    public string Id => "WinMm.RecordToFile";
    public string Description => "Record audio via WaveIn for a fixed duration";
    public MenuPath? MenuLocation =>
        new("WinMM (Windows Multimedia)", "Record audio (WaveIn)", Group: "Playback & Recording", Order: 1);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("output", typeof(string), Required: false,
            Help: "output WAV path (auto-named under Desktop\\NAudio\\ if blank)"),
        new("duration", typeof(TimeSpan), Required: false, Default: TimeSpan.FromSeconds(10),
            Help: "recording duration"),
        new("deviceNumber", typeof(int), Required: false, Default: 0, Help: "WaveIn device index"),
        new("sampleRate", typeof(int), Required: false, Default: 44100, Help: "PCM sample rate"),
        new("bitsPerSample", typeof(int), Required: false, Default: 16, Help: "bits per sample"),
        new("channels", typeof(int), Required: false, Default: 2, Help: "channel count"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var deviceCount = WaveIn.DeviceCount;
        if (deviceCount == 0)
            return TestResult.Fail("No WaveIn recording devices found");

        var deviceNumber = ctx.Get<int>("deviceNumber");
        if (deviceNumber < 0 || deviceNumber >= deviceCount)
            return TestResult.Fail($"deviceNumber {deviceNumber} out of range (0..{deviceCount - 1})");

        var duration = ctx.Get<TimeSpan>("duration");
        var sampleRate = ctx.Get<int>("sampleRate");
        var bitsPerSample = ctx.Get<int>("bitsPerSample");
        var channels = ctx.Get<int>("channels");

        ctx.TryGet<string>("output", out var outputPath);
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "NAudio");
            Directory.CreateDirectory(dir);
            outputPath = Path.Combine(dir, $"winmm_recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav");
        }
        else
        {
            var parent = Path.GetDirectoryName(Path.GetFullPath(outputPath));
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);
        }

        AnsiConsole.MarkupLine($"[grey]WaveIn devices:[/] {deviceCount}");
        for (var n = 0; n < deviceCount; n++)
        {
            var caps = WaveIn.GetCapabilities(n);
            var marker = n == deviceNumber ? "[yellow]→[/]" : " ";
            AnsiConsole.MarkupLine($"  {marker} {n}: {Markup.Escape(caps.ProductName)} ({caps.Channels}ch)");
        }

        using var waveIn = new WaveIn
        {
            DeviceNumber = deviceNumber,
            WaveFormat = new WaveFormat(sampleRate, bitsPerSample, channels),
            BufferMilliseconds = 100,
            NumberOfBuffers = 3,
        };

        AnsiConsole.MarkupLine($"[grey]Device:[/]   {WaveIn.GetCapabilities(deviceNumber).ProductName}");
        AnsiConsole.MarkupLine($"[grey]Format:[/]   {waveIn.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Output:[/]   {Markup.Escape(outputPath)}");
        AnsiConsole.MarkupLine($"[grey]Duration:[/] {duration.TotalSeconds:F0}s");
        AnsiConsole.WriteLine();

        using var writer = new WaveFileWriter(outputPath, waveIn.WaveFormat);
        long totalBytes = 0;
        waveIn.DataAvailable += (_, e) =>
        {
            writer.Write(e.Buffer, 0, e.BytesRecorded);
            Interlocked.Add(ref totalBytes, e.BytesRecorded);
        };

        AnsiConsole.MarkupLine("[green]Recording...[/] [dim](Ctrl+C to stop early)[/]");
        waveIn.StartRecording();

        var start = DateTime.UtcNow;
        var elapsed = TimeSpan.Zero;
        while (elapsed < duration)
        {
            if (ctx.Cancellation.WaitHandle.WaitOne(100)) break;
            elapsed = DateTime.UtcNow - start;
        }

        waveIn.StopRecording();
        writer.Flush();

        var cancelled = ctx.Cancellation.IsCancellationRequested;
        var outputInfo = new FileInfo(outputPath);
        AnsiConsole.MarkupLine($"\n[grey]Recorded {totalBytes:N0} bytes in {elapsed.TotalSeconds:F1}s[/]");
        AnsiConsole.MarkupLine($"[grey]File size:[/] {outputInfo.Length:N0} bytes");

        var diagnostics = new Dictionary<string, string>
        {
            ["outputPath"] = outputPath,
            ["outputBytes"] = outputInfo.Length.ToString(),
            ["pcmBytes"] = totalBytes.ToString(),
            ["elapsedMs"] = elapsed.TotalMilliseconds.ToString("F0"),
            ["deviceNumber"] = deviceNumber.ToString(),
        };

        if (cancelled)
            return TestResult.Skipped($"Cancelled after {elapsed.TotalSeconds:F1}s");

        return totalBytes == 0
            ? TestResult.Fail("Recording produced zero bytes — device may be silent or misconfigured", diagnostics)
            : TestResult.Pass($"Recorded {totalBytes:N0} bytes in {elapsed.TotalSeconds:F1}s", diagnostics);
    }
}
