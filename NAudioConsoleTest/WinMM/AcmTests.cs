using NAudio.FileFormats.Mp3;
using NAudio.Wave;
using NAudioConsoleTest.Shared;
using Spectre.Console;

namespace NAudioConsoleTest.WinMM;

static class AcmTests
{
    public static void DecodeMp3()
    {
        AnsiConsole.MarkupLine("[bold]Decode MP3 with AcmMp3FrameDecompressor[/]\n");

        var inputPath = AudioFileSelector.SelectAudioFile();
        if (inputPath == null) return;

        if (!inputPath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
        {
            AnsiConsole.MarkupLine("[yellow]Warning: This test is designed for MP3 files[/]");
        }

        var outputPath = Path.Combine(
            Path.GetDirectoryName(inputPath)!,
            Path.GetFileNameWithoutExtension(inputPath) + "_acm_decoded.wav");

        int readIterations = 0;
        long totalBytesDecoded = 0;

        AnsiConsole.MarkupLine($"[grey]Input:[/]  {Markup.Escape(Path.GetFileName(inputPath))}");
        AnsiConsole.MarkupLine($"[grey]Output:[/] {Markup.Escape(Path.GetFileName(outputPath))}");
        AnsiConsole.MarkupLine("");

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Decoding MP3 via ACM...", ctx =>
            {
                using var reader = new Mp3FileReaderBase(inputPath, wf => new AcmMp3FrameDecompressor(wf));
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

    public static void ConvertWithProvider()
    {
        AnsiConsole.MarkupLine("[bold]Convert Format with WaveFormatConversionProvider[/]\n");

        var inputPath = AudioFileSelector.SelectAudioFile();
        if (inputPath == null) return;

        var outputPath = Path.Combine(
            Path.GetDirectoryName(inputPath)!,
            Path.GetFileNameWithoutExtension(inputPath) + "_acm_converted.wav");

        long totalBytes = 0;

        AnsiConsole.MarkupLine($"[grey]Input:[/]  {Markup.Escape(Path.GetFileName(inputPath))}");
        AnsiConsole.MarkupLine($"[grey]Output:[/] {Markup.Escape(Path.GetFileName(outputPath))}");
        AnsiConsole.MarkupLine("");

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Converting format via ACM...", ctx =>
            {
                using var reader = new WaveFileReader(inputPath);
                AnsiConsole.MarkupLine($"[grey]Source format:[/] {reader.WaveFormat}");

                // Resample to a different sample rate
                int targetRate = reader.WaveFormat.SampleRate == 44100 ? 22050 : 44100;
                var targetFormat = new WaveFormat(targetRate, reader.WaveFormat.BitsPerSample, reader.WaveFormat.Channels);
                AnsiConsole.MarkupLine($"[grey]Target format:[/] {targetFormat}");

                var resampler = new WaveFormatConversionProvider(targetFormat, reader);
                using var writer = new WaveFileWriter(outputPath, resampler.WaveFormat);

                var buffer = new byte[targetFormat.AverageBytesPerSecond];
                int bytesRead;
                while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
                {
                    writer.Write(buffer, 0, bytesRead);
                    totalBytes += bytesRead;
                }
            });

        var outputInfo = new FileInfo(outputPath);
        AnsiConsole.MarkupLine($"[green]Converted successfully[/]");
        AnsiConsole.MarkupLine($"[grey]PCM bytes:[/]   {totalBytes:N0}");
        AnsiConsole.MarkupLine($"[grey]Output size:[/] {outputInfo.Length:N0} bytes");

        using var verifyReader = new WaveFileReader(outputPath);
        AnsiConsole.MarkupLine($"[grey]Output format:[/]   {verifyReader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Output duration:[/] {verifyReader.TotalTime:hh\\:mm\\:ss\\.fff}");

        AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
    }

    public static void ConvertWithStream()
    {
        AnsiConsole.MarkupLine("[bold]Convert Format with WaveFormatConversionStream[/]\n");

        var inputPath = AudioFileSelector.SelectAudioFile();
        if (inputPath == null) return;

        if (!inputPath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
        {
            AnsiConsole.MarkupLine("[yellow]Warning: WaveFormatConversionStream works best with WAV input[/]");
        }

        var outputPath = Path.Combine(
            Path.GetDirectoryName(inputPath)!,
            Path.GetFileNameWithoutExtension(inputPath) + "_acm_stream_converted.wav");

        long totalBytes = 0;

        AnsiConsole.MarkupLine($"[grey]Input:[/]  {Markup.Escape(Path.GetFileName(inputPath))}");
        AnsiConsole.MarkupLine($"[grey]Output:[/] {Markup.Escape(Path.GetFileName(outputPath))}");
        AnsiConsole.MarkupLine("");

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Converting format via ACM stream...", ctx =>
            {
                using var reader = new WaveFileReader(inputPath);
                AnsiConsole.MarkupLine($"[grey]Source format:[/] {reader.WaveFormat}");

                // Resample to a different sample rate
                int targetRate = reader.WaveFormat.SampleRate == 44100 ? 22050 : 44100;
                var targetFormat = new WaveFormat(targetRate, reader.WaveFormat.BitsPerSample, reader.WaveFormat.Channels);
                AnsiConsole.MarkupLine($"[grey]Target format:[/] {targetFormat}");

                using var conversionStream = new WaveFormatConversionStream(targetFormat, reader);
                using var writer = new WaveFileWriter(outputPath, conversionStream.WaveFormat);

                var buffer = new byte[conversionStream.WaveFormat.AverageBytesPerSecond];
                int bytesRead;
                while ((bytesRead = conversionStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    writer.Write(buffer, 0, bytesRead);
                    totalBytes += bytesRead;
                }
            });

        var outputInfo = new FileInfo(outputPath);
        AnsiConsole.MarkupLine($"[green]Converted successfully[/]");
        AnsiConsole.MarkupLine($"[grey]PCM bytes:[/]   {totalBytes:N0}");
        AnsiConsole.MarkupLine($"[grey]Output size:[/] {outputInfo.Length:N0} bytes");

        using var verifyReader = new WaveFileReader(outputPath);
        AnsiConsole.MarkupLine($"[grey]Output format:[/]   {verifyReader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Output duration:[/] {verifyReader.TotalTime:hh\\:mm\\:ss\\.fff}");

        AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
    }
}
