using NAudio.FileFormats.Mp3;
using NAudio.Wave;
using NAudioConsoleTest.Shared;
using Spectre.Console;

namespace NAudioConsoleTest.WinMM;

static class PlaybackTests
{
    public static void PlayFile()
    {
        AnsiConsole.MarkupLine("[bold]Play Audio File with WaveOut[/]\n");

        // Show available devices
        int deviceCount = WaveOut.DeviceCount;
        AnsiConsole.MarkupLine($"[grey]WaveOut devices found:[/] {deviceCount}");
        for (int n = -1; n < deviceCount; n++)
        {
            var caps = WaveOut.GetCapabilities(n);
            AnsiConsole.MarkupLine($"[grey]  {n}: {Markup.Escape(caps.ProductName)} ({caps.Channels}ch)[/]");
        }
        AnsiConsole.MarkupLine("");

        var inputPath = AudioFileSelector.SelectAudioFile();
        if (inputPath == null) return;

        using var reader = new Mp3FileReaderBase(inputPath, wf => new AcmMp3FrameDecompressor(wf));
        AnsiConsole.MarkupLine($"[grey]Format:[/]   {reader.WaveFormat}");
        AnsiConsole.MarkupLine($"[grey]Duration:[/] {reader.TotalTime:hh\\:mm\\:ss\\.fff}");
        AnsiConsole.MarkupLine("");

        using var waveOut = new WaveOut
        {
            DeviceNumber = -1,
            BufferMilliseconds = 100,
            NumberOfBuffers = 2
        };

        waveOut.Init(reader);
        waveOut.Volume = 0.5f; // safe volume

        AnsiConsole.MarkupLine("[green]Playing...[/] [dim]Press SPACE to pause/resume, ESC to stop[/]");
        waveOut.Play();

        while (waveOut.PlaybackState != PlaybackState.Stopped)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Escape)
                {
                    waveOut.Stop();
                    break;
                }
                if (key.Key == ConsoleKey.Spacebar)
                {
                    if (waveOut.PlaybackState == PlaybackState.Playing)
                    {
                        waveOut.Pause();
                        AnsiConsole.MarkupLine("[yellow]Paused[/]");
                    }
                    else if (waveOut.PlaybackState == PlaybackState.Paused)
                    {
                        waveOut.Play();
                        AnsiConsole.MarkupLine("[green]Resumed[/]");
                    }
                }
            }
            Thread.Sleep(100);
        }

        AnsiConsole.MarkupLine("[dim]Playback stopped[/]");
        AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
    }
}
