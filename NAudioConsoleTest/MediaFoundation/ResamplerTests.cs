using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudioConsoleTest.Shared;
using Spectre.Console;

namespace NAudioConsoleTest.MediaFoundation;

static class ResamplerTests
{
    public static void ResampleFile()
    {
        AnsiConsole.MarkupLine("[bold]Resample Audio File[/]\n");

        var inputPath = AudioFileSelector.SelectAudioFile();
        if (inputPath == null) return;

        MediaFoundationApi.Startup();

        using var reader = new MediaFoundationReader(inputPath);

        AnsiConsole.MarkupLine($"[grey]Input:[/]       {Markup.Escape(Path.GetFileName(inputPath))}");
        AnsiConsole.MarkupLine($"[grey]Format:[/]      {reader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Duration:[/]    {reader.TotalTime:hh\\:mm\\:ss\\.fff}");

        var targetRate = AnsiConsole.Prompt(
            new SelectionPrompt<int>()
                .Title("Target sample rate:")
                .AddChoices(8000, 16000, 22050, 44100, 48000, 96000));

        var outputPath = Path.Combine(
            Path.GetDirectoryName(inputPath)!,
            Path.GetFileNameWithoutExtension(inputPath) + $"_resampled_{targetRate}.wav");

        var outputFormat = new WaveFormat(targetRate, reader.WaveFormat.BitsPerSample, reader.WaveFormat.Channels);

        AnsiConsole.MarkupLine($"[grey]Output rate:[/] {targetRate} Hz");
        AnsiConsole.MarkupLine($"[grey]Output:[/]      {Markup.Escape(Path.GetFileName(outputPath))}");

        long totalBytesWritten = 0;
        AnsiConsole.MarkupLine("");
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Resampling...", ctx =>
            {
                using var resampler = new MediaFoundationResampler(reader, outputFormat);
                using var writer = new WaveFileWriter(outputPath, resampler.WaveFormat);

                var buffer = new byte[resampler.WaveFormat.AverageBytesPerSecond];
                int bytesRead;
                while ((bytesRead = resampler.Read(buffer.AsSpan())) > 0)
                {
                    writer.Write(buffer, 0, bytesRead);
                    totalBytesWritten += bytesRead;
                }
            });

        var outputInfo = new FileInfo(outputPath);
        AnsiConsole.MarkupLine($"[green]Resampled successfully[/]");
        AnsiConsole.MarkupLine($"[grey]Output size:[/]   {outputInfo.Length:N0} bytes");
        AnsiConsole.MarkupLine($"[grey]Bytes written:[/] {totalBytesWritten:N0}");

        // Verify output
        using var verifyReader = new WaveFileReader(outputPath);
        AnsiConsole.MarkupLine($"[grey]Output format:[/]   {verifyReader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Output duration:[/] {verifyReader.TotalTime:hh\\:mm\\:ss\\.fff}");

        AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
    }
}
