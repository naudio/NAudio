using NAudio.Wave;
using Spectre.Console;

namespace NAudioConsoleTest.Asio;

static class AsioLifecycleTests
{
    public static void ReinitializeRoundTrip()
    {
        AnsiConsole.MarkupLine("[bold]Reinitialize round-trip (Phase C / Phase 0 F6)[/]\n");
        AnsiConsole.MarkupLine("[grey]Records ~1s of silence, Stop()s, Reinitialize()s, Start()s again, records ~1s more.[/]");
        AnsiConsole.MarkupLine("[grey]Verifies the same AsioDevice instance can recover the way DriverResetRequest expects.[/]\n");

        var driverName = AsioDeviceSelector.SelectDriver();
        if (driverName == null) return;

        using var device = AsioDevice.Open(driverName);
        if (device.Capabilities.NbInputChannels == 0)
        {
            AnsiConsole.MarkupLine("[red]This driver has no input channels.[/]");
            PressAnyKey();
            return;
        }

        device.InitRecording(new AsioRecordingOptions
        {
            InputChannels = [0],
            SampleRate = device.CurrentSampleRate
        });

        int firstPassCallbacks = 0;
        int secondPassCallbacks = 0;
        int phase = 1;
        device.AudioCaptured += (_, _) =>
        {
            if (phase == 1) Interlocked.Increment(ref firstPassCallbacks);
            else Interlocked.Increment(ref secondPassCallbacks);
        };

        // Pass 1
        device.Start();
        Thread.Sleep(1000);
        device.Stop();
        AnsiConsole.MarkupLine($"  Pass 1: {firstPassCallbacks} callbacks fired before Stop().");

        // Pass 2 — same options, no recreation of the AsioDevice
        try
        {
            device.Reinitialize();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Reinitialize threw: {Markup.Escape(ex.Message)}[/]");
            PressAnyKey();
            return;
        }

        phase = 2;
        device.Start();
        Thread.Sleep(1000);
        device.Stop();
        AnsiConsole.MarkupLine($"  Pass 2: {secondPassCallbacks} callbacks fired after Reinitialize+Start.");

        if (firstPassCallbacks > 0 && secondPassCallbacks > 0)
            AnsiConsole.MarkupLine("[green]PASS — device produced audio on both passes after Reinitialize.[/]");
        else
            AnsiConsole.MarkupLine("[red]FAIL — at least one pass produced no callbacks.[/]");

        PressAnyKey();
    }

    private static void PressAnyKey()
    {
        AnsiConsole.MarkupLine("\n[dim]Press any key...[/]");
        Console.ReadKey(intercept: true);
    }
}
