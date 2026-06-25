using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.WinMM.Tests;

/// <summary>
/// Plays an audio file through <see cref="WaveOut"/>. Interactive mode: ESC stops, SPACE
/// toggles pause/resume. Non-interactive: runs until end-of-stream or until
/// <c>maxDuration</c> elapses, with Ctrl+C as the abort path.
/// </summary>
internal sealed class WinMmPlayFileTest : IConsoleTest
{
    public string Id => "WinMm.PlayFile";
    public string Description => "Play an audio file via WaveOut";
    public MenuPath? MenuLocation =>
        new("WinMM (Windows Multimedia)", "Play audio file (WaveOut)", Group: "Playback & Recording", Order: 0);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("input", typeof(string), Required: true, Help: "input audio file path (MP3/WAV)", IsFilePath: true, FileCategory: "audio"),
        new("deviceNumber", typeof(int), Required: false, Default: -1,
            Help: "WaveOut device index (-1 = WAVE_MAPPER / default)"),
        new("volume", typeof(float), Required: false, Default: 0.5f, Help: "output volume (0..1)"),
        new("maxDuration", typeof(TimeSpan), Required: false, Default: TimeSpan.FromMinutes(5),
            Help: "playback cap (interactive mode uses ESC instead)", CliOnly: true),
    ];

    public TestResult Run(TestContext ctx)
    {
        var inputPath = ctx.Get<string>("input");
        if (!File.Exists(inputPath)) return TestResult.Fail($"Input not found: {inputPath}");

        var deviceNumber = ctx.Get<int>("deviceNumber");
        var volume = ctx.Get<float>("volume");
        var maxDuration = ctx.Get<TimeSpan>("maxDuration");

        var deviceCount = WaveOut.DeviceCount;
        AnsiConsole.MarkupLine($"[grey]WaveOut devices:[/] {deviceCount}");
        for (var n = -1; n < deviceCount; n++)
        {
            var caps = WaveOut.GetCapabilities(n);
            var marker = n == deviceNumber ? "[yellow]→[/]" : " ";
            AnsiConsole.MarkupLine($"  {marker} {n}: {Markup.Escape(caps.ProductName)} ({caps.Channels}ch)");
        }
        AnsiConsole.WriteLine();

        using var reader = OpenReader(inputPath);
        AnsiConsole.MarkupLine($"[grey]Input:[/]    {Markup.Escape(Path.GetFileName(inputPath))}");
        AnsiConsole.MarkupLine($"[grey]Format:[/]   {reader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Duration:[/] {reader.TotalTime:hh\\:mm\\:ss\\.fff}");

        using var waveOut = new WaveOut
        {
            DeviceNumber = deviceNumber,
            BufferMilliseconds = 100,
            NumberOfBuffers = 2,
        };
        waveOut.Init(reader);
        waveOut.Volume = volume;

        AnsiConsole.MarkupLine(ctx.Interactive
            ? "[green]Playing[/] [dim](SPACE pause/resume, ESC stop)[/]"
            : $"[green]Playing[/] [dim](cap {maxDuration.TotalSeconds:F0}s, Ctrl+C to stop early)[/]");
        var start = DateTime.UtcNow;
        waveOut.Play();

        while (waveOut.PlaybackState != PlaybackState.Stopped)
        {
            if (ctx.Interactive && Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Escape)
                {
                    waveOut.Stop();
                    break;
                }
                if (key.Key == ConsoleKey.Spacebar)
                {
                    if (waveOut.PlaybackState == PlaybackState.Playing)
                    {
                        waveOut.Pause();
                        AnsiConsole.MarkupLine("[yellow]Paused[/]");
                    }
                    else if (waveOut.PlaybackState == PlaybackState.Paused)
                    {
                        waveOut.Play();
                        AnsiConsole.MarkupLine("[green]Resumed[/]");
                    }
                }
            }
            if (ctx.Cancellation.WaitHandle.WaitOne(100))
            {
                waveOut.Stop();
                break;
            }
            // maxDuration is a CLI-only cap; in interactive mode the user controls "stop" via ESC.
            if (!ctx.Interactive && DateTime.UtcNow - start >= maxDuration)
            {
                waveOut.Stop();
                break;
            }
        }

        var elapsed = DateTime.UtcNow - start;
        var cancelled = ctx.Cancellation.IsCancellationRequested;
        if (cancelled)
            return TestResult.Skipped($"Cancelled after {elapsed.TotalSeconds:F1}s");

        return TestResult.Pass(
            $"Played for {elapsed.TotalSeconds:F1}s",
            new Dictionary<string, string>
            {
                ["deviceNumber"] = deviceNumber.ToString(),
                ["elapsedMs"] = elapsed.TotalMilliseconds.ToString("F0"),
                ["fileDurationMs"] = reader.TotalTime.TotalMilliseconds.ToString("F0"),
            });
    }

    private static WaveStream OpenReader(string path)
    {
        // Mp3FileReaderBase covers MP3 with the ACM decompressor; WaveFileReader handles WAV.
        return path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)
            ? new Mp3FileReaderBase(path, wf => new AcmMp3FrameDecompressor(wf))
            : new WaveFileReader(path);
    }
}
