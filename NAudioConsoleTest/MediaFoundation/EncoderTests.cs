using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudioConsoleTest.Shared;
using Spectre.Console;

namespace NAudioConsoleTest.MediaFoundation;

static class EncoderTests
{
    public static void EncodeToMp3() => EncodeFile("MP3", ".mp3", MediaFoundationEncoder.EncodeToMp3);
    public static void EncodeToAac() => EncodeFile("AAC", ".mp4", MediaFoundationEncoder.EncodeToAac);
    public static void EncodeToWma() => EncodeFile("WMA", ".wma", MediaFoundationEncoder.EncodeToWma);

    private static void EncodeFile(string formatName, string extension,
        Action<IAudioSource, string, int> encode)
    {
        AnsiConsole.MarkupLine($"[bold]Encode to {formatName}[/]\n");

        var inputPath = AudioFileSelector.SelectAudioFile();
        if (inputPath == null) return;

        MediaFoundationApi.Startup();

        var outputPath = Path.Combine(
            Path.GetDirectoryName(inputPath)!,
            Path.GetFileNameWithoutExtension(inputPath) + $"_encoded{extension}");

        // Show available bitrates
        using var reader = new MediaFoundationReader(inputPath);

        AnsiConsole.MarkupLine($"[grey]Input:[/]       {Markup.Escape(Path.GetFileName(inputPath))}");
        AnsiConsole.MarkupLine($"[grey]Input format:[/] {reader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Duration:[/]     {reader.TotalTime:hh\\:mm\\:ss\\.fff}");
        AnsiConsole.MarkupLine($"[grey]Output:[/]       {Markup.Escape(Path.GetFileName(outputPath))}");

        var desiredBitrate = AnsiConsole.Prompt(
            new TextPrompt<int>("Desired bitrate (bps):").DefaultValue(192000));

        AnsiConsole.MarkupLine("");
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start($"Encoding to {formatName}...", ctx =>
            {
                encode(reader, outputPath, desiredBitrate);
            });

        var outputInfo = new FileInfo(outputPath);
        AnsiConsole.MarkupLine($"[green]Encoded successfully[/]");
        AnsiConsole.MarkupLine($"[grey]Output size:[/] {outputInfo.Length:N0} bytes");

        // Verify the output file can be read back
        using var verifyReader = new MediaFoundationReader(outputPath);
        AnsiConsole.MarkupLine($"[grey]Output format:[/] {verifyReader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Output duration:[/] {verifyReader.TotalTime:hh\\:mm\\:ss\\.fff}");

        AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
    }
}
