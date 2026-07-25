using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace AsioStressTest;

// AsioStressTest - reproduction harness for the ASIO access-violation-after-teardown fault.
//
// Hypothesis under test (see README.md for the full analysis):
//   The four ASIO callback delegates (bufferSwitch / bufferSwitchTimeInfo / asioMessage /
//   sampleRateDidChange) are rooted only by AsioDriverExt, which is rooted only by AsioDevice.
//   After Stop()+Dispose() the wrapper is abandoned; Marshal.Release does NOT stop the driver's
//   notification thread. A GC then collects the delegates, and the next asioMessage(kAsioResetRequest)
//   the driver fires - e.g. when the Windows master volume changes - calls a dangling thunk => 0xC0000005.
//
// This harness automates exactly the manual sequence: play a short quiet segment on ASIO,
// stop + dispose, force a GC, then nudge the master volume to provoke the driver reset callback.
//
// Build & run in Release:
//   dotnet run -c Release --project tests/AsioStressTest -- --iterations 100
//
// Useful switches:
//   --driver <name|index>   ASIO driver to use (default: 0 = first installed)
//   --iterations N          number of open/play/stop/dispose cycles (default 100)
//   --play-ms N             ms of audio to play each cycle (default 250)
//   --settle-ms N           ms to wait after the volume nudge for a late callback (default 150)
//   --volume f              playback gain 0..1, kept low to save your ears (default 0.03)
//   --no-gc                 skip the forced GC (should NOT crash - control case)
//   --no-toggle             skip the master-volume nudge
//   --keep-alive            root every disposed AsioOut so the delegates survive GC
//                           (should NOT crash - isolates the cause to delegate collection)
//   --list                  list installed ASIO drivers and exit
//
// Exit codes:
//   0  = completed all iterations with no access violation
//   1  = argument / setup error, or no ASIO driver present
//   <native> = 0xC0000005 access violation (repro achieved) - OS-determined, process dies
internal static class Program
{
    // When --keep-alive is set, disposed players are parked here so the GC can never reclaim
    // their callback delegates. If that makes the crash disappear, the diagnosis is confirmed.
    private static readonly List<object> KeptAlive = new();
    private static readonly MMDeviceEnumerator enumerator = new();

    [STAThread]
    private static int Main(string[] args)
    {
        var o = Options.Parse(args);
        if (o is null) return 1;

        var drivers = AsioOut.GetDriverNames();
        if (o.ListOnly)
        {
            Console.WriteLine($"Installed ASIO drivers ({drivers.Length}):");
            for (int i = 0; i < drivers.Length; i++) Console.WriteLine($"  [{i}] {drivers[i]}");
            return drivers.Length == 0 ? 1 : 0;
        }

        if (drivers.Length == 0)
        {
            Console.Error.WriteLine("No ASIO driver installed on this system.");
            return 1;
        }

        string? driverName = ResolveDriver(o.Driver, drivers);
        if (driverName is null)
        {
            Console.Error.WriteLine($"Could not resolve driver '{o.Driver}'. Use --list to see options.");
            return 1;
        }

        Console.WriteLine("=== AsioStressTest ===");
        Console.WriteLine($"driver       : {driverName}");
        Console.WriteLine($"iterations   : {o.Iterations}");
        Console.WriteLine($"play-ms      : {o.PlayMs}");
        Console.WriteLine($"settle-ms    : {o.SettleMs}");
        Console.WriteLine($"volume       : {o.Volume}");
        Console.WriteLine($"force GC     : {(o.ForceGc ? "yes" : "no")}");
        Console.WriteLine($"toggle vol   : {(o.ToggleVolume ? "yes" : "no")}");
        Console.WriteLine($"keep-alive   : {(o.KeepAlive ? "yes (delegates rooted - control)" : "no (delegates abandoned)")}");
        Console.WriteLine($"pid          : {Environment.ProcessId}");
        Console.WriteLine("If this crashes with 0xC0000005 the fault is reproduced.");
        Console.WriteLine();

        AudioEndpointVolume? endpointVolume = o.ToggleVolume ? TryGetMasterVolume() : null;
        float originalVolume = endpointVolume?.MasterVolumeLevelScalar ?? 0f;

        var sw = Stopwatch.StartNew();
        WeakReference? lastPlayer = null;

        try
        {
            for (int i = 1; i <= o.Iterations; i++)
            {
                // Report whether the PREVIOUS iteration's player has actually been collected.
                // A "collected" report here means its callback delegates are now dangling if the
                // driver still holds their thunks - i.e. we are in the danger window.
                string prevState = lastPlayer is null
                    ? "-"
                    : (lastPlayer.IsAlive ? "still-alive" : "COLLECTED");

                AsioOut? player = PlayOneCycle(driverName, o);

                if (o.KeepAlive) KeptAlive.Add(player);
                lastPlayer = new WeakReference(player);
                player = null; // drop our strong reference so it becomes collectable (unless kept alive)

                if (o.ForceGc)
                {
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
                    GC.WaitForPendingFinalizers();
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
                }

                // Provoke the driver's kAsioResetRequest by changing the endpoint the driver watches.
                if (endpointVolume != null)
                {
                    Thread.Sleep(o.SettleMs);
                    NudgeVolume(endpointVolume, originalVolume, o);
                    // Give the driver's notification thread time to fire the (now possibly dangling) callback.
                    Thread.Sleep(o.SettleMs);
                }

                Console.WriteLine($"[{i,4}/{o.Iterations}] ok  (prev player: {prevState})");
            }
        }
        finally
        {
            if (endpointVolume != null)
            {
                try { endpointVolume.MasterVolumeLevelScalar = originalVolume; } catch { /* best effort */ }
                endpointVolume.Dispose();
            }
        }

        Console.WriteLine();
        Console.WriteLine($"DONE - {o.Iterations} iterations in {sw.Elapsed.TotalSeconds:F1}s with no access violation.");
        return 0;
    }

    private static AsioOut PlayOneCycle(string driverName, Options o)
    {
        var player = new AsioOut(driverName) { AutoStop = false };

        int sampleRate = PickSampleRate(player);
        var source = new SignalGenerator(sampleRate, 2)
        {
            Type = SignalGeneratorType.Sin,
            Frequency = 440,
            Gain = o.Volume,
        };

        player.Init(source.ToWaveProvider());
        player.Play();
        Thread.Sleep(o.PlayMs);
        player.Stop();
        player.Dispose();
        return player;
    }

    private static int PickSampleRate(AsioOut player)
    {
        foreach (var rate in new[] { 48000, 44100, 96000, 88200 })
        {
            try { if (player.IsSampleRateSupported(rate)) return rate; }
            catch { /* some drivers throw on unsupported query - try the next */ }
        }
        return 48000; // last resort; Init will surface a clear error if truly unsupported
    }

    private static string? ResolveDriver(string spec, string[] drivers)
    {
        if (string.IsNullOrWhiteSpace(spec)) return drivers[0];
        if (int.TryParse(spec, out int index))
            return index >= 0 && index < drivers.Length ? drivers[index] : null;
        foreach (var d in drivers)
            if (string.Equals(d, spec, StringComparison.OrdinalIgnoreCase)) return d;
        return null;
    }

    private static AudioEndpointVolume? TryGetMasterVolume()
    {
        try
        {
            enumerator.RegisterEndpointNotificationCallback(new NotificationClient());
            var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            return device.AudioEndpointVolume;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"WARN: could not access master volume ({ex.Message}); volume nudging disabled.");
            return null;
        }
    }

    // Nudge the master volume down a hair and back. On shared-mode / WASAPI-backed ASIO drivers
    // this endpoint change makes the driver raise kAsioResetRequest - the callback we suspect is dangling.
    private static void NudgeVolume(AudioEndpointVolume vol, float original, Options o)
    {
        try
        {
            float nudged = original >= 0.5f ? original - 0.05f : original + 0.05f;
            vol.MasterVolumeLevelScalar = Math.Clamp(nudged, 0f, 1f);
            Thread.Sleep(o.SettleMs);
            vol.MasterVolumeLevelScalar = original;
        }
        catch { /* endpoint may have changed under us - not our concern here */ }
    }
}

