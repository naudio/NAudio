using NAudio.Dmo.Effect;
using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudioConsoleTest.Shared;
using Spectre.Console;

namespace NAudioConsoleTest.Dmo;

static class DmoEffectTests
{
    public static void ApplyEcho()
    {
        AnsiConsole.MarkupLine("[bold]Apply Echo Effect with DmoEffectWaveProvider[/]\n");

        var inputPath = AudioFileSelector.SelectAudioFile();
        if (inputPath == null) return;

        var outputPath = Path.Combine(
            Path.GetDirectoryName(inputPath)!,
            Path.GetFileNameWithoutExtension(inputPath) + "_dmo_echo.wav");

        AnsiConsole.MarkupLine($"[grey]Input:[/]  {Markup.Escape(Path.GetFileName(inputPath))}");
        AnsiConsole.MarkupLine($"[grey]Output:[/] {Markup.Escape(Path.GetFileName(outputPath))}");

        long totalBytesWritten = 0;
        AnsiConsole.MarkupLine("");
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Applying echo effect via DMO...", ctx =>
            {
                MediaFoundationApi.Startup();

                // DMO effects require 16-bit PCM. Use MediaFoundationReader for broad format support.
                using var reader = new MediaFoundationReader(inputPath);

                // If the reader didn't give us 16-bit PCM, we can't use DMO effects directly
                if (reader.WaveFormat.Encoding != WaveFormatEncoding.Pcm || reader.WaveFormat.BitsPerSample != 16)
                {
                    AnsiConsole.MarkupLine($"[yellow]Input decoded as {reader.WaveFormat} - DMO effects require 16-bit PCM[/]");
                    AnsiConsole.MarkupLine("[yellow]Try a 16-bit WAV file[/]");
                    return;
                }

                using var echo = new DmoEffectWaveProvider<DmoEcho, DmoEcho.Params>(reader);

                AnsiConsole.MarkupLine($"[grey]Format:[/]    {reader.WaveFormat}");
                AnsiConsole.MarkupLine($"[grey]Wet/Dry:[/]   {echo.EffectParams.WetDryMix:F1}%");
                AnsiConsole.MarkupLine($"[grey]Feedback:[/]  {echo.EffectParams.FeedBack:F1}%");
                AnsiConsole.MarkupLine($"[grey]Delay L:[/]   {echo.EffectParams.LeftDelay:F1}ms");
                AnsiConsole.MarkupLine($"[grey]Delay R:[/]   {echo.EffectParams.RightDelay:F1}ms");

                using var writer = new WaveFileWriter(outputPath, echo.WaveFormat);

                var buffer = new byte[echo.WaveFormat.AverageBytesPerSecond];
                int bytesRead;
                while ((bytesRead = echo.Read(buffer, 0, buffer.Length)) > 0)
                {
                    writer.Write(buffer, 0, bytesRead);
                    totalBytesWritten += bytesRead;
                }
            });

        if (totalBytesWritten == 0) return;

        var outputInfo = new FileInfo(outputPath);
        AnsiConsole.MarkupLine($"[green]Echo effect applied successfully[/]");
        AnsiConsole.MarkupLine($"[grey]Output size:[/] {outputInfo.Length:N0} bytes");

        using var verifyReader = new WaveFileReader(outputPath);
        AnsiConsole.MarkupLine($"[grey]Output format:[/]   {verifyReader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Output duration:[/] {verifyReader.TotalTime:hh\\:mm\\:ss\\.fff}");

        AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
    }
}
