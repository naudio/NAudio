using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.MediaFoundation.Tests;

/// <summary>
/// Resamples an audio file to a target sample rate via <see cref="MediaFoundationResampler"/>
/// and writes the result to a WAV file.
/// </summary>
sealed class MediaFoundationResampleFileTest : IConsoleTest
{
    public string Id => "MediaFoundation.ResampleFile";
    public string Description => "Resample an audio file to a target sample rate (WAV output)";
    public MenuPath? MenuLocation =>
        new("Media Foundation", "Resample audio file", Group: "Resampling", Order: 0);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("input", typeof(string), Required: true, Help: "input audio file path"),
        new("output", typeof(string), Required: false, Help: "output WAV path (auto if blank)"),
        new("targetRate", typeof(int), Required: false, Default: 44100,
            Help: "target sample rate in Hz",
            Choices: ["8000", "16000", "22050", "44100", "48000", "96000"]),
    ];

    public TestResult Run(TestContext ctx)
    {
        var inputPath = ctx.Get<string>("input");
        if (!File.Exists(inputPath))
            return TestResult.Fail($"Input not found: {inputPath}");

        var targetRate = ctx.Get<int>("targetRate");
        ctx.TryGet<string>("output", out var outputPath);
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(inputPath))!,
                Path.GetFileNameWithoutExtension(inputPath) + $"_resampled_{targetRate}.wav");
        }

        MediaFoundationApi.Startup();
        using var reader = new MediaFoundationReader(inputPath);

        AnsiConsole.MarkupLine($"[grey]Input:[/]       {Markup.Escape(Path.GetFileName(inputPath))}");
        AnsiConsole.MarkupLine($"[grey]Format:[/]      {reader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Duration:[/]    {reader.TotalTime:hh\\:mm\\:ss\\.fff}");
        AnsiConsole.MarkupLine($"[grey]Output rate:[/] {targetRate} Hz");
        AnsiConsole.MarkupLine($"[grey]Output:[/]      {Markup.Escape(outputPath)}");
        AnsiConsole.WriteLine();

        var outputFormat = new WaveFormat(targetRate, reader.WaveFormat.BitsPerSample, reader.WaveFormat.Channels);

        long totalBytesWritten = 0;
        void DoResample()
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
        }

        if (ctx.Interactive)
            AnsiConsole.Status().Spinner(Spinner.Known.Dots).Start("Resampling...", _ => DoResample());
        else
            DoResample();

        var outputInfo = new FileInfo(outputPath);
        using var verifyReader = new WaveFileReader(outputPath);
        AnsiConsole.MarkupLine($"[green]Resampled successfully[/]");
        AnsiConsole.MarkupLine($"[grey]Output size:[/]     {outputInfo.Length:N0} bytes");
        AnsiConsole.MarkupLine($"[grey]Output format:[/]   {verifyReader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Output duration:[/] {verifyReader.TotalTime:hh\\:mm\\:ss\\.fff}");

        return TestResult.Pass(
            $"Resampled to {targetRate} Hz, {outputInfo.Length:N0} bytes",
            new Dictionary<string, string>
            {
                ["outputPath"] = outputPath,
                ["outputBytes"] = outputInfo.Length.ToString(),
                ["bytesWritten"] = totalBytesWritten.ToString(),
                ["targetRate"] = targetRate.ToString(),
                ["outputDurationMs"] = verifyReader.TotalTime.TotalMilliseconds.ToString("F0"),
            });
    }
}
