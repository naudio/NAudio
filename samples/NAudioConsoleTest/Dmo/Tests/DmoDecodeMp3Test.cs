using NAudio.Dmo;
using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Dmo.Tests;

sealed class DmoDecodeMp3Test : IConsoleTest
{
    public string Id => "Dmo.DecodeMp3";
    public string Description => "Decode an MP3 file via DmoMp3FrameDecompressor and write the PCM to WAV";
    public MenuPath? MenuLocation => new("DMO (DirectX Media Objects)", "Decode MP3 (DmoMp3FrameDecompressor)", Order: 1);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("input", typeof(string), Required: true, Help: "input MP3 file path"),
        new("output", typeof(string), Required: false, Help: "output WAV path (auto if blank)"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var inputPath = ctx.Get<string>("input");
        if (!File.Exists(inputPath)) return TestResult.Fail($"Input not found: {inputPath}");
        if (!inputPath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
            AnsiConsole.MarkupLine("[yellow]Warning: this test is designed for MP3 files[/]");

        ctx.TryGet<string>("output", out var outputPath);
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(inputPath))!,
                Path.GetFileNameWithoutExtension(inputPath) + "_dmo_decoded.wav");
        }

        AnsiConsole.MarkupLine($"[grey]Input:[/]  {Markup.Escape(Path.GetFileName(inputPath))}");
        AnsiConsole.MarkupLine($"[grey]Output:[/] {Markup.Escape(outputPath)}");
        AnsiConsole.WriteLine();

        int readIterations = 0;
        long totalBytesDecoded = 0;
        void DoDecode()
        {
            using var reader = new Mp3FileReaderBase(inputPath, wf => new DmoMp3FrameDecompressor(wf));
            using var writer = new WaveFileWriter(outputPath, reader.WaveFormat);
            AnsiConsole.MarkupLine($"[grey]PCM format:[/] {reader.WaveFormat}");
            AnsiConsole.MarkupLine($"[grey]Duration:[/]   {reader.TotalTime:hh\\:mm\\:ss\\.fff}");

            var buffer = new byte[reader.WaveFormat.AverageBytesPerSecond];
            int bytesRead;
            while (!ctx.Cancellation.IsCancellationRequested
                   && (bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                writer.Write(buffer, 0, bytesRead);
                totalBytesDecoded += bytesRead;
                readIterations++;
            }
        }

        if (ctx.Interactive)
            AnsiConsole.Status().Spinner(Spinner.Known.Dots).Start("Decoding MP3 via DMO...", _ => DoDecode());
        else
            DoDecode();

        if (ctx.Cancellation.IsCancellationRequested)
            return TestResult.Skipped("Cancelled");

        var outputInfo = new FileInfo(outputPath);
        using var verifyReader = new WaveFileReader(outputPath);
        AnsiConsole.MarkupLine($"[green]Decoded successfully[/]");
        AnsiConsole.MarkupLine($"[grey]Output size:[/]     {outputInfo.Length:N0} bytes");
        AnsiConsole.MarkupLine($"[grey]Output format:[/]   {verifyReader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Output duration:[/] {verifyReader.TotalTime:hh\\:mm\\:ss\\.fff}");

        return TestResult.Pass(
            $"Decoded {totalBytesDecoded:N0} PCM bytes in {readIterations} reads",
            new Dictionary<string, string>
            {
                ["outputPath"] = outputPath,
                ["outputBytes"] = outputInfo.Length.ToString(),
                ["pcmBytes"] = totalBytesDecoded.ToString(),
                ["readIterations"] = readIterations.ToString(),
            });
    }
}
