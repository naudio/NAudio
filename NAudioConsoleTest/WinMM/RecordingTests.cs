using NAudio.Wave;
using Spectre.Console;

namespace NAudioConsoleTest.WinMM;

static class RecordingTests
{
    public static void RecordToFile()
    {
        AnsiConsole.MarkupLine("[bold]Record Audio with WaveIn[/]\n");

        // Show available devices
        int deviceCount = WaveIn.DeviceCount;
        if (deviceCount == 0)
        {
            AnsiConsole.MarkupLine("[red]No WaveIn recording devices found[/]");
            AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
            Console.ReadKey(intercept: true);
            return;
        }

        AnsiConsole.MarkupLine($"[grey]WaveIn devices found:[/] {deviceCount}");
        for (int n = 0; n < deviceCount; n++)
        {
            var caps = WaveIn.GetCapabilities(n);
            AnsiConsole.MarkupLine($"[grey]  {n}: {Markup.Escape(caps.ProductName)} ({caps.Channels}ch)[/]");
        }
        AnsiConsole.MarkupLine("");

        var outputPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "NAudio",
            $"winmm_recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        using var waveIn = new WaveIn
        {
            DeviceNumber = 0,
            WaveFormat = new WaveFormat(44100, 16, 2),
            BufferMilliseconds = 100,
            NumberOfBuffers = 3
        };

        AnsiConsole.MarkupLine($"[grey]Device:[/]  {WaveIn.GetCapabilities(waveIn.DeviceNumber).ProductName}");
        AnsiConsole.MarkupLine($"[grey]Format:[/]  {waveIn.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Output:[/]  {Markup.Escape(Path.GetFileName(outputPath))}");
        AnsiConsole.MarkupLine("");

        using var writer = new WaveFileWriter(outputPath, waveIn.WaveFormat);
        long totalBytes = 0;

        waveIn.DataAvailable += (s, e) =>
        {
            writer.Write(e.Buffer, 0, e.BytesRecorded);
            totalBytes += e.BytesRecorded;
        };

        AnsiConsole.MarkupLine("[green]Recording...[/] [dim]Press any key to stop (max 10 seconds)[/]");
        waveIn.StartRecording();

        var startTime = DateTime.UtcNow;
        while (!Console.KeyAvailable && (DateTime.UtcNow - startTime).TotalSeconds < 10)
        {
            var elapsed = DateTime.UtcNow - startTime;
            Console.Write($"\r  Elapsed: {elapsed:mm\\:ss\\.f}  Bytes: {totalBytes:N0}    ");
            Thread.Sleep(100);
        }
        if (Console.KeyAvailable) Console.ReadKey(intercept: true);

        waveIn.StopRecording();
        writer.Dispose(); // flush before checking file

        Console.WriteLine();
        var outputInfo = new FileInfo(outputPath);
        AnsiConsole.MarkupLine($"[green]Recording complete[/]");
        AnsiConsole.MarkupLine($"[grey]PCM bytes:[/]   {totalBytes:N0}");
        AnsiConsole.MarkupLine($"[grey]File size:[/]   {outputInfo.Length:N0} bytes");
        AnsiConsole.MarkupLine($"[grey]File:[/]        {Markup.Escape(outputPath)}");

        AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
    }
}
