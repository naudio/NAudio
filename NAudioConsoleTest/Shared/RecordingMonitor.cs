using Spectre.Console;
using System.Diagnostics;

namespace NAudioConsoleTest.Shared;

/// <summary>
/// Displays real-time recording status with countdown or open-ended timing.
/// </summary>
static class RecordingMonitor
{
    /// <summary>
    /// Monitors a recording with a fixed duration countdown.
    /// Returns when duration elapses or user presses SPACE/ESC.
    /// </summary>
    /// <returns>true if completed normally, false if user interrupted</returns>
    public static bool MonitorWithCountdown(string deviceName, TimeSpan duration, Action stop)
    {
        var sw = Stopwatch.StartNew();

        AnsiConsole.MarkupLine($"[bold red]Recording:[/] {duration.TotalSeconds:0}s");
        AnsiConsole.MarkupLine($"[grey]Device:[/] {Markup.Escape(deviceName)}");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("[dim]SPACE = stop early  |  ESC = cancel[/]");
        AnsiConsole.MarkupLine("");

        while (sw.Elapsed < duration)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Spacebar)
                {
                    stop();
                    Console.WriteLine();
                    AnsiConsole.MarkupLine("[yellow]Stopped early[/]");
                    return true;
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    stop();
                    Console.WriteLine();
                    AnsiConsole.MarkupLine("[red]Cancelled[/]");
                    return false;
                }
            }

            var remaining = duration - sw.Elapsed;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
            Console.Write($"\r  Remaining: {remaining:mm\\:ss\\.f}  Elapsed: {sw.Elapsed:mm\\:ss\\.f}    ");
            Thread.Sleep(100);
        }

        stop();
        Console.WriteLine();
        AnsiConsole.MarkupLine("[green]Recording complete[/]");
        return true;
    }

    /// <summary>
    /// Monitors an open-ended recording until user presses SPACE or ESC.
    /// </summary>
    /// <returns>true if stopped normally (SPACE), false if cancelled (ESC)</returns>
    public static bool MonitorUntilStopped(string deviceName, Action stop)
    {
        var sw = Stopwatch.StartNew();

        AnsiConsole.MarkupLine($"[bold red]Recording...[/]");
        AnsiConsole.MarkupLine($"[grey]Device:[/] {Markup.Escape(deviceName)}");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("[dim]SPACE = stop recording  |  ESC = cancel[/]");
        AnsiConsole.MarkupLine("");

        while (true)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Spacebar)
                {
                    stop();
                    Console.WriteLine();
                    AnsiConsole.MarkupLine("[green]Recording stopped[/]");
                    return true;
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    stop();
                    Console.WriteLine();
                    AnsiConsole.MarkupLine("[red]Cancelled[/]");
                    return false;
                }
            }

            Console.Write($"\r  Elapsed: {sw.Elapsed:mm\\:ss\\.f}    ");
            Thread.Sleep(100);
        }
    }
}
