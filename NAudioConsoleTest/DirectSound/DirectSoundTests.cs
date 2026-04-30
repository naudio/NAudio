using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Spectre.Console;

namespace NAudioConsoleTest.DirectSound;

static class DirectSoundTests
{
    public static void ListDevices()
    {
        AnsiConsole.MarkupLine("[bold]DirectSound Devices[/]\n");

        var table = new Table()
            .AddColumn("GUID")
            .AddColumn("Description")
            .AddColumn("Module");

        int count = 0;
        foreach (var device in DirectSoundOut.Devices)
        {
            table.AddRow(
                Markup.Escape(device.Guid.ToString()),
                Markup.Escape(device.Description ?? string.Empty),
                Markup.Escape(device.ModuleName ?? string.Empty));
            count++;
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[grey]Enumerated {count} device(s).[/]");
        AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
    }

    public static void PlayTone()
    {
        AnsiConsole.MarkupLine("[bold]Play tone with DirectSoundOut[/]\n");

        // Short, low-volume sine — confirms DirectSoundCreate, IDirectSound, primary +
        // secondary buffer creation, the IDirectSoundNotify QI cascade, the playback
        // notification thread, and the Feed loop. Audible signal so the user can
        // verify "yes, the migration didn't break playback".
        var tone = new SignalGenerator(44100, 1)
        {
            Frequency = 440,
            Gain = 0.10,
            Type = SignalGeneratorType.Sin,
        }.Take(TimeSpan.FromSeconds(2)).ToWaveProvider();

        using var dsoundOut = new DirectSoundOut(40);
        dsoundOut.Init(tone);

        AnsiConsole.MarkupLine("[green]Playing 440 Hz tone for 2 seconds...[/] [dim]ESC to stop early[/]");
        dsoundOut.Play();

        while (dsoundOut.PlaybackState != PlaybackState.Stopped)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Escape)
                {
                    dsoundOut.Stop();
                    break;
                }
            }
            Thread.Sleep(50);
        }

        AnsiConsole.MarkupLine("[dim]Playback stopped[/]");
        AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
    }
}
