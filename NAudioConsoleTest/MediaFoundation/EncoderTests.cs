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

    /// <summary>
    /// Exercises the EncodeToXxx(IWaveProvider, Stream, int) overloads against an
    /// in-memory MemoryStream, then reads the result back via StreamMediaFoundationReader.
    /// This is the full ComStream CCW round-trip — both the write leg
    /// (CreateSinkWriter(ComStream, Guid) → MFCreateMFByteStreamOnStream with QI-for-IID
    /// on the IStream CCW) and the read leg (CreateByteStream + CreateSourceReaderFromByteStream).
    /// Runs all three encoder formats in sequence so MP4-container (AAC) and ASF-container
    /// (WMA) byte-stream patterns get the same coverage as MP3.
    /// </summary>
    public static void RoundTripThroughMemoryStream()
    {
        AnsiConsole.MarkupLine("[bold]Round-trip encode through MemoryStream (MP3 + AAC + WMA)[/]\n");
        AnsiConsole.MarkupLine("[dim]Exercises EncodeToXxx(IWaveProvider, Stream, int) overloads " +
                               "and StreamMediaFoundationReader. Covers both legs of the ComStream " +
                               "CCW path for all three encoder container types.[/]\n");

        var inputPath = AudioFileSelector.SelectAudioFile();
        if (inputPath == null) return;

        MediaFoundationApi.Startup();

        var bitrate = AnsiConsole.Prompt(
            new TextPrompt<int>("Bitrate (bps):").DefaultValue(96000));

        var formats = new (string Name, Action<IWaveProvider, Stream, int> Encode)[]
        {
            ("MP3", MediaFoundationEncoder.EncodeToMp3),
            ("AAC", MediaFoundationEncoder.EncodeToAac),
            ("WMA", MediaFoundationEncoder.EncodeToWma),
        };

        foreach (var (name, encode) in formats)
        {
            AnsiConsole.MarkupLine($"\n[bold yellow]-- {name} --[/]");
            try
            {
                // Fresh reader per format because IWaveProvider input gets consumed.
                using var source = new MediaFoundationReader(inputPath);

                using var encoded = new MemoryStream();
                encode(source, encoded, bitrate);
                AnsiConsole.MarkupLine($"  [green]Encoded:[/] {encoded.Length:N0} bytes (CCW write leg)");

                encoded.Position = 0;
                using var verifyReader = new StreamMediaFoundationReader(encoded);
                var buffer = new byte[verifyReader.WaveFormat.AverageBytesPerSecond];
                long total = 0;
                int bytesRead;
                while ((bytesRead = verifyReader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    total += bytesRead;
                }
                AnsiConsole.MarkupLine($"  [green]Decoded:[/] {total:N0} bytes PCM via " +
                                       $"{Markup.Escape(verifyReader.WaveFormat.ToString())} (CCW read leg)");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"  [red]FAILED:[/] {Markup.Escape(ex.GetType().Name)}: " +
                                       $"{Markup.Escape(ex.Message)}");
            }
        }

        AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
    }

    private static void EncodeFile(string formatName, string extension,
        Action<IWaveProvider, string, int> encode)
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
