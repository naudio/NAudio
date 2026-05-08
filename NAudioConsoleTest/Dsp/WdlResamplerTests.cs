using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudioConsoleTest.Shared;
using Spectre.Console;

namespace NAudioConsoleTest.Dsp;

static class WdlResamplerTests
{
    public static void ResampleFile()
    {
        AnsiConsole.MarkupLine("[bold]Resample with WdlResamplingSampleProvider[/]\n");

        var inputPath = AudioFileSelector.SelectAudioFile();
        if (inputPath == null) return;

        MediaFoundationApi.Startup();
        using var reader = new MediaFoundationReader(inputPath);
        var sampleSource = reader.ToSampleProvider();

        AnsiConsole.MarkupLine($"[grey]Input:[/]       {Markup.Escape(Path.GetFileName(inputPath))}");
        AnsiConsole.MarkupLine($"[grey]Format:[/]      {reader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Duration:[/]    {reader.TotalTime:hh\\:mm\\:ss\\.fff}");

        var targetRate = AnsiConsole.Prompt(
            new SelectionPrompt<int>()
                .Title("Target sample rate:")
                .AddChoices(8000, 16000, 22050, 44100, 48000, 96000));

        var outputPath = Path.Combine(
            Path.GetDirectoryName(inputPath)!,
            Path.GetFileNameWithoutExtension(inputPath) + $"_wdl_resampled_{targetRate}.wav");

        AnsiConsole.MarkupLine($"[grey]Output rate:[/] {targetRate} Hz");
        AnsiConsole.MarkupLine($"[grey]Output:[/]      {Markup.Escape(Path.GetFileName(outputPath))}");

        long totalBytesWritten = 0;
        AnsiConsole.MarkupLine("");
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Resampling via WDL...", ctx =>
            {
                var resampler = new WdlResamplingSampleProvider(sampleSource, targetRate);
                var waveProvider = new SampleToWaveProvider(resampler);
                using var writer = new WaveFileWriter(outputPath, waveProvider.WaveFormat);

                var buffer = new byte[waveProvider.WaveFormat.AverageBytesPerSecond];
                int bytesRead;
                while ((bytesRead = waveProvider.Read(buffer.AsSpan())) > 0)
                {
                    writer.Write(buffer, 0, bytesRead);
                    totalBytesWritten += bytesRead;
                }
            });

        var outputInfo = new FileInfo(outputPath);
        AnsiConsole.MarkupLine($"[green]Resampled successfully[/]");
        AnsiConsole.MarkupLine($"[grey]Output size:[/]   {outputInfo.Length:N0} bytes");
        AnsiConsole.MarkupLine($"[grey]Bytes written:[/] {totalBytesWritten:N0}");

        using var verifyReader = new WaveFileReader(outputPath);
        AnsiConsole.MarkupLine($"[grey]Output format:[/]   {verifyReader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Output duration:[/] {verifyReader.TotalTime:hh\\:mm\\:ss\\.fff}");

        AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
    }
}
