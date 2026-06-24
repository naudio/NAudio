using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.WinMM.Tests;

internal sealed class WinMmConvertWithProviderTest : IConsoleTest
{
    public string Id => "WinMm.ConvertWithProvider";
    public string Description => "Convert a WAV file's sample rate via WaveFormatConversionProvider (ACM)";
    public MenuPath? MenuLocation =>
        new("WinMM (Windows Multimedia)", "Convert format (WaveFormatConversionProvider)",
            Group: "ACM Compression", Order: 1);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("input", typeof(string), Required: true, Help: "input WAV file path"),
        new("output", typeof(string), Required: false, Help: "output WAV path (auto if blank)"),
        new("targetRate", typeof(int), Required: false, Default: 0,
            Help: "target sample rate in Hz (0 = swap between 44.1/22.05k based on input)"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var inputPath = ctx.Get<string>("input");
        if (!File.Exists(inputPath)) return TestResult.Fail($"Input not found: {inputPath}");

        ctx.TryGet<string>("output", out var outputPath);
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(inputPath))!,
                Path.GetFileNameWithoutExtension(inputPath) + "_acm_converted.wav");
        }

        var requestedRate = ctx.Get<int>("targetRate");

        AnsiConsole.MarkupLine($"[grey]Input:[/]  {Markup.Escape(Path.GetFileName(inputPath))}");
        AnsiConsole.MarkupLine($"[grey]Output:[/] {Markup.Escape(outputPath)}");
        AnsiConsole.WriteLine();

        long totalBytes = 0;
        WaveFormat? targetFormat = null;
        void DoConvert()
        {
            using var reader = new WaveFileReader(inputPath);
            AnsiConsole.MarkupLine($"[grey]Source format:[/] {reader.WaveFormat}");

            var rate = requestedRate > 0
                ? requestedRate
                : (reader.WaveFormat.SampleRate == 44100 ? 22050 : 44100);
            targetFormat = new WaveFormat(rate, reader.WaveFormat.BitsPerSample, reader.WaveFormat.Channels);
            AnsiConsole.MarkupLine($"[grey]Target format:[/] {targetFormat}");

            using var resampler = new WaveFormatConversionProvider(targetFormat, reader);
            using var writer = new WaveFileWriter(outputPath, resampler.WaveFormat);

            var buffer = new byte[targetFormat.AverageBytesPerSecond];
            int bytesRead;
            while (!ctx.Cancellation.IsCancellationRequested
                   && (bytesRead = resampler.Read(buffer.AsSpan())) > 0)
            {
                writer.Write(buffer, 0, bytesRead);
                totalBytes += bytesRead;
            }
        }

        if (ctx.Interactive)
            AnsiConsole.Status().Spinner(Spinner.Known.Dots).Start("Converting format via ACM...", _ => DoConvert());
        else
            DoConvert();

        if (ctx.Cancellation.IsCancellationRequested)
            return TestResult.Skipped("Cancelled");

        var outputInfo = new FileInfo(outputPath);
        using var verifyReader = new WaveFileReader(outputPath);
        AnsiConsole.MarkupLine($"[green]Converted successfully[/]");
        AnsiConsole.MarkupLine($"[grey]Output size:[/]     {outputInfo.Length:N0} bytes");
        AnsiConsole.MarkupLine($"[grey]Output format:[/]   {verifyReader.WaveFormat}");

        return TestResult.Pass(
            $"Converted to {targetFormat?.SampleRate} Hz, {outputInfo.Length:N0} bytes",
            new Dictionary<string, string>
            {
                ["outputPath"] = outputPath,
                ["outputBytes"] = outputInfo.Length.ToString(),
                ["pcmBytes"] = totalBytes.ToString(),
                ["targetRate"] = targetFormat?.SampleRate.ToString() ?? "?",
            });
    }
}
