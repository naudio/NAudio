using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Dsp.Tests;

sealed class DspWdlResampleFileTest : IConsoleTest
{
    public string Id => "Dsp.WdlResampleFile";
    public string Description => "Resample an audio file via WdlResamplingSampleProvider (managed WDL resampler)";
    public MenuPath? MenuLocation => new("DSP", "Resample audio file (WdlResamplingSampleProvider)");

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("input", typeof(string), Required: true, Help: "input audio file path"),
        new("output", typeof(string), Required: false, Help: "output WAV path (auto if blank)"),
        new("targetRate", typeof(int), Required: false, Default: 44100, Help: "target sample rate in Hz",
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
                Path.GetFileNameWithoutExtension(inputPath) + $"_wdl_resampled_{targetRate}.wav");
        }

        MediaFoundationApi.Startup();
        using var reader = new MediaFoundationReader(inputPath);
        var sampleSource = reader.ToSampleProvider();

        AnsiConsole.MarkupLine($"[grey]Input:[/]       {Markup.Escape(Path.GetFileName(inputPath))}");
        AnsiConsole.MarkupLine($"[grey]Format:[/]      {reader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Duration:[/]    {reader.TotalTime:hh\\:mm\\:ss\\.fff}");
        AnsiConsole.MarkupLine($"[grey]Output rate:[/] {targetRate} Hz");
        AnsiConsole.MarkupLine($"[grey]Output:[/]      {Markup.Escape(outputPath)}");
        AnsiConsole.WriteLine();

        long totalBytesWritten = 0;
        void DoResample()
        {
            var resampler = new WdlResamplingSampleProvider(sampleSource, targetRate);
            var waveProvider = new SampleToWaveProvider(resampler);
            using var writer = new WaveFileWriter(outputPath, waveProvider.WaveFormat);
            var buffer = new byte[waveProvider.WaveFormat.AverageBytesPerSecond];
            int bytesRead;
            while (!ctx.Cancellation.IsCancellationRequested
                   && (bytesRead = waveProvider.Read(buffer.AsSpan())) > 0)
            {
                writer.Write(buffer, 0, bytesRead);
                totalBytesWritten += bytesRead;
            }
        }

        if (ctx.Interactive)
            AnsiConsole.Status().Spinner(Spinner.Known.Dots).Start("Resampling via WDL...", _ => DoResample());
        else
            DoResample();

        if (ctx.Cancellation.IsCancellationRequested)
            return TestResult.Skipped("Cancelled");

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
            });
    }
}
