using NAudio.CoreAudioApi;
using Spectre.Console;

namespace NAudioConsoleTest.Wasapi;

// Stress-tests the IAudioEndpointVolumeCallback CCW dispatch path that Phase 2f
// rebuilt on top of [GeneratedComInterface] / [GeneratedComClass] / ComWrappers.
// A regression here looks like an access-violation on the WASAPI worker thread
// the first time a callback fires after MasterVolumeLevelScalar is set — the
// process exits with 0xC0000005 and no managed exception. Subscriber output
// proves the callback chain executes end-to-end.
static class VolumeCallbackStressTest
{
    public static void Run()
    {
        using var enumerator = new MMDeviceEnumerator();
        var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        AnsiConsole.MarkupLine($"Device: [yellow]{Markup.Escape(device.FriendlyName)}[/]");

        var endpointVolume = device.AudioEndpointVolume;
        float original = endpointVolume.MasterVolumeLevelScalar;
        AnsiConsole.MarkupLine($"Original master volume: [yellow]{original:F3}[/]\n");

        int notifyCount = 0;
        endpointVolume.OnVolumeNotification += data =>
        {
            Interlocked.Increment(ref notifyCount);
        };

        try
        {
            float[] levels = { 0.50f, 0.30f, 0.50f, 0.70f, 0.50f };
            const int iterations = 10;
            int totalSets = 0;
            for (int iter = 0; iter < iterations; iter++)
            {
                foreach (var lvl in levels)
                {
                    endpointVolume.MasterVolumeLevelScalar = lvl;
                    totalSets++;
                    Thread.Sleep(50);
                }
            }
            // Give any in-flight callbacks a moment to land.
            Thread.Sleep(200);
            AnsiConsole.MarkupLine($"[green]Done.[/] {totalSets} master-volume changes; {notifyCount} callbacks fired.");
            if (notifyCount == 0)
            {
                AnsiConsole.MarkupLine("[yellow]Warning: zero callbacks fired — the CCW may be registered but not dispatching.[/]");
            }
        }
        finally
        {
            try { endpointVolume.MasterVolumeLevelScalar = original; } catch { }
            Thread.Sleep(200);
            endpointVolume.Dispose();
            device.Dispose();
        }

        AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
    }
}
