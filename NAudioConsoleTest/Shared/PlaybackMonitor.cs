using NAudio.Wave;
using Spectre.Console;
using System.Diagnostics;

namespace NAudioConsoleTest.Shared;

/// <summary>
/// Displays real-time playback status and handles keyboard input (space=pause, esc=stop).
/// </summary>
static class PlaybackMonitor
{
    public static void Monitor(WasapiPlayer player, string deviceName, string description)
    {
        var sw = Stopwatch.StartNew();

        AnsiConsole.MarkupLine($"[bold green]Playing:[/] {Markup.Escape(description)}");
        AnsiConsole.MarkupLine($"[grey]Device:[/]  {Markup.Escape(deviceName)}");
        AnsiConsole.MarkupLine($"[grey]Format:[/]  {player.OutputWaveFormat}");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("[dim]SPACE = pause/resume  |  ESC = stop[/]");
        AnsiConsole.MarkupLine("");

        while (player.PlaybackState != PlaybackState.Stopped)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Spacebar)
                {
                    if (player.PlaybackState == PlaybackState.Playing)
                    {
                        player.Pause();
                        AnsiConsole.MarkupLine("[yellow]Paused[/]");
                    }
                    else if (player.PlaybackState == PlaybackState.Paused)
                    {
                        player.Play();
                        AnsiConsole.MarkupLine("[green]Resumed[/]");
                    }
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    player.Stop();
                    AnsiConsole.MarkupLine("[red]Stopped[/]");
                    break;
                }
            }

            var elapsed = sw.Elapsed;
            Console.Write($"\r  Elapsed: {elapsed:mm\\:ss\\.f}  State: {player.PlaybackState}    ");
            Thread.Sleep(100);
        }

        Console.WriteLine();
        AnsiConsole.MarkupLine($"[dim]Playback finished ({sw.Elapsed:mm\\:ss\\.f})[/]");
    }
}
