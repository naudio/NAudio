using NAudio.SoundFile;
using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.SoundFile.Tests;

/// <summary>
/// Decodes any libsndfile-supported file with <see cref="SoundFileReader"/>
/// and plays it through <see cref="WaveOut"/>. Interactive: SPACE
/// pause/resume, ESC stop. Non-interactive: runs to end-of-stream or until
/// <c>maxDuration</c>.
/// </summary>
internal sealed class SoundFilePlayFileTest : IConsoleTest
{
    public string Id => "SoundFile.PlayFile";
    public string Description => "Play an audio file decoded via libsndfile (FLAC/Ogg/Opus/MP3/WAV/…)";
    public MenuPath? MenuLocation =>
        new("Sound File (libsndfile)", "Play audio file", Group: "Playback", Order: 0);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("input", typeof(string), Required: true, Help: "input audio file path", IsFilePath: true, FileCategory: "audio"),
        new("volume", typeof(float), Required: false, Default: 0.5f, Help: "output volume (0..1)"),
        new("maxDuration", typeof(TimeSpan), Required: false, Default: TimeSpan.FromMinutes(2),
            Help: "playback cap (interactive mode uses ESC instead)", CliOnly: true),
    ];

    public TestResult Run(TestContext ctx)
    {
        if (SoundFileTestHelper.ProbeLibrary() is { } skip)
        {
            return skip;
        }

        var inputPath = ctx.Get<string>("input");
        if (!File.Exists(inputPath))
        {
            return TestResult.Fail($"Input not found: {inputPath}");
        }
        var volume = ctx.Get<float>("volume");
        var maxDuration = ctx.Get<TimeSpan>("maxDuration");

        SoundFileReader reader;
        try
        {
            reader = new SoundFileReader(inputPath);
        }
        catch (SoundFileException ex)
        {
            return TestResult.Fail($"libsndfile could not open '{Path.GetFileName(inputPath)}': {ex.Message}");
        }

        using (reader)
        {
            AnsiConsole.MarkupLine($"[grey]Input:[/]    {Markup.Escape(Path.GetFileName(inputPath))}");
            AnsiConsole.MarkupLine($"[grey]Format:[/]   {reader.WaveFormat}");
            AnsiConsole.MarkupLine($"[grey]Duration:[/] {reader.TotalTime:hh\\:mm\\:ss\\.fff}");
            if (!reader.Tags.IsEmpty)
            {
                AnsiConsole.MarkupLine($"[grey]Tags:[/]     {Markup.Escape($"{reader.Tags.Artist} – {reader.Tags.Title}")}");
            }

            using var output = new WaveOut();
            output.Init(reader);
            output.Volume = volume;

            AnsiConsole.MarkupLine(ctx.Interactive
                ? "[green]Playing[/] [dim](SPACE pause/resume, ESC stop)[/]"
                : $"[green]Playing[/] [dim](cap {maxDuration.TotalSeconds:F0}s, Ctrl+C to stop early)[/]");
            var start = DateTime.UtcNow;
            output.Play();

            while (output.PlaybackState != PlaybackState.Stopped)
            {
                if (ctx.Interactive && Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.Escape)
                    {
                        output.Stop();
                        break;
                    }
                    if (key.Key == ConsoleKey.Spacebar)
                    {
                        if (output.PlaybackState == PlaybackState.Playing)
                        {
                            output.Pause();
                            AnsiConsole.MarkupLine("[yellow]Paused[/]");
                        }
                        else
                        {
                            output.Play();
                            AnsiConsole.MarkupLine("[green]Resumed[/]");
                        }
                    }
                }
                if (ctx.Cancellation.WaitHandle.WaitOne(100))
                {
                    output.Stop();
                    break;
                }
                if (!ctx.Interactive && DateTime.UtcNow - start >= maxDuration)
                {
                    output.Stop();
                    break;
                }
            }

            var elapsed = DateTime.UtcNow - start;
            if (ctx.Cancellation.IsCancellationRequested)
            {
                return TestResult.Skipped($"Cancelled after {elapsed.TotalSeconds:F1}s");
            }

            return TestResult.Pass(
                $"Played for {elapsed.TotalSeconds:F1}s",
                new Dictionary<string, string>
                {
                    ["elapsedMs"] = elapsed.TotalMilliseconds.ToString("F0"),
                    ["fileDurationMs"] = reader.TotalTime.TotalMilliseconds.ToString("F0"),
                    ["format"] = reader.WaveFormat.ToString(),
                });
        }
    }
}
