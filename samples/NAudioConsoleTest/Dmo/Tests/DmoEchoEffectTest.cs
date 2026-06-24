using NAudio.Dmo.Effect;
using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Dmo.Tests;

internal sealed class DmoEchoEffectTest : IConsoleTest
{
    public string Id => "Dmo.EchoEffect";
    public string Description => "Apply DMO echo effect to an audio file";
    public MenuPath? MenuLocation => new("DMO (DirectX Media Objects)", "Apply echo effect (DmoEffectWaveProvider)", Order: 2);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("input", typeof(string), Required: true, Help: "input audio file path"),
        new("output", typeof(string), Required: false, Help: "output WAV path (auto if blank)"),
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
                Path.GetFileNameWithoutExtension(inputPath) + "_dmo_echo.wav");
        }

        AnsiConsole.MarkupLine($"[grey]Input:[/]  {Markup.Escape(Path.GetFileName(inputPath))}");
        AnsiConsole.MarkupLine($"[grey]Output:[/] {Markup.Escape(outputPath)}");
        AnsiConsole.WriteLine();

        MediaFoundationApi.Startup();

        // DMO effects need 16-bit PCM. MediaFoundationReader gives us broad codec support but may
        // emit IEEE float for some inputs; in that case we have to skip rather than crash inside the DMO.
        using var reader = new MediaFoundationReader(inputPath);
        if (reader.WaveFormat.Encoding != WaveFormatEncoding.Pcm || reader.WaveFormat.BitsPerSample != 16)
            return TestResult.Skipped(
                $"DMO effects require 16-bit PCM; reader produced {reader.WaveFormat}");

        long totalBytesWritten = 0;
        DmoEcho.Params echoParams = default;
        void DoApply()
        {
            using var echo = new DmoEffectWaveProvider<DmoEcho, DmoEcho.Params>(reader);
            echoParams = echo.EffectParams;
            AnsiConsole.MarkupLine($"[grey]Format:[/]    {reader.WaveFormat}");
            AnsiConsole.MarkupLine($"[grey]Wet/Dry:[/]   {echoParams.WetDryMix:F1}%");
            AnsiConsole.MarkupLine($"[grey]Feedback:[/]  {echoParams.FeedBack:F1}%");
            AnsiConsole.MarkupLine($"[grey]Delay L:[/]   {echoParams.LeftDelay:F1}ms");
            AnsiConsole.MarkupLine($"[grey]Delay R:[/]   {echoParams.RightDelay:F1}ms");

            using var writer = new WaveFileWriter(outputPath, echo.WaveFormat);
            var buffer = new byte[echo.WaveFormat.AverageBytesPerSecond];
            int bytesRead;
            while (!ctx.Cancellation.IsCancellationRequested
                   && (bytesRead = echo.Read(buffer.AsSpan())) > 0)
            {
                writer.Write(buffer, 0, bytesRead);
                totalBytesWritten += bytesRead;
            }
        }

        if (ctx.Interactive)
            AnsiConsole.Status().Spinner(Spinner.Known.Dots).Start("Applying echo effect via DMO...", _ => DoApply());
        else
            DoApply();

        if (ctx.Cancellation.IsCancellationRequested)
            return TestResult.Skipped("Cancelled");

        var outputInfo = new FileInfo(outputPath);
        using var verifyReader = new WaveFileReader(outputPath);
        AnsiConsole.MarkupLine($"[green]Echo applied successfully[/]");
        AnsiConsole.MarkupLine($"[grey]Output size:[/]     {outputInfo.Length:N0} bytes");
        AnsiConsole.MarkupLine($"[grey]Output format:[/]   {verifyReader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Output duration:[/] {verifyReader.TotalTime:hh\\:mm\\:ss\\.fff}");

        return TestResult.Pass(
            $"Applied echo, {outputInfo.Length:N0} bytes",
            new Dictionary<string, string>
            {
                ["outputPath"] = outputPath,
                ["outputBytes"] = outputInfo.Length.ToString(),
                ["wetDryMix"] = echoParams.WetDryMix.ToString("F1"),
                ["feedback"] = echoParams.FeedBack.ToString("F1"),
            });
    }
}
