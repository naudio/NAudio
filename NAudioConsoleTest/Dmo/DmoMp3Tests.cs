using NAudio.FileFormats.Mp3;
using NAudio.Wave;
using NAudioConsoleTest.Shared;
using Spectre.Console;

namespace NAudioConsoleTest.Dmo;

static class DmoMp3Tests
{
    public static void DecodeMp3()
    {
        AnsiConsole.MarkupLine("[bold]Decode MP3 with DmoMp3FrameDecompressor[/]\n");

        var inputPath = AudioFileSelector.SelectAudioFile();
        if (inputPath == null) return;

        if (!inputPath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
        {
            AnsiConsole.MarkupLine("[yellow]Warning: This test is designed for MP3 files[/]");
        }

        var outputPath = Path.Combine(
            Path.GetDirectoryName(inputPath)!,
            Path.GetFileNameWithoutExtension(inputPath) + "_dmo_decoded.wav");

        int readIterations = 0;
        long totalBytesDecoded = 0;

        AnsiConsole.MarkupLine($"[grey]Input:[/]  {Markup.Escape(Path.GetFileName(inputPath))}");
        AnsiConsole.MarkupLine($"[grey]Output:[/] {Markup.Escape(Path.GetFileName(outputPath))}");
        AnsiConsole.MarkupLine("");

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Decoding MP3 via DMO...", ctx =>
            {
                using var reader = new Mp3FileReaderBase(inputPath, wf => new DmoMp3FrameDecompressor(wf));
                using var writer = new WaveFileWriter(outputPath, reader.WaveFormat);

                AnsiConsole.MarkupLine($"[grey]PCM format:[/] {reader.WaveFormat}");
                AnsiConsole.MarkupLine($"[grey]Duration:[/]   {reader.TotalTime:hh\\:mm\\:ss\\.fff}");

                var buffer = new byte[reader.WaveFormat.AverageBytesPerSecond];
                int bytesRead;
                while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    writer.Write(buffer, 0, bytesRead);
                    totalBytesDecoded += bytesRead;
                    readIterations++;
                }
            });

        var outputInfo = new FileInfo(outputPath);
        AnsiConsole.MarkupLine($"[green]Decoded successfully[/]");
        AnsiConsole.MarkupLine($"[grey]Read iterations:[/] {readIterations}");
        AnsiConsole.MarkupLine($"[grey]PCM bytes:[/]      {totalBytesDecoded:N0}");
        AnsiConsole.MarkupLine($"[grey]Output size:[/]    {outputInfo.Length:N0} bytes");

        using var verifyReader = new WaveFileReader(outputPath);
        AnsiConsole.MarkupLine($"[grey]Output format:[/]   {verifyReader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Output duration:[/] {verifyReader.TotalTime:hh\\:mm\\:ss\\.fff}");

        AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
    }
}
