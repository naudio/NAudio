using System.Diagnostics;
using MfStressTest;
using MfStressTest.Phases;
using NAudio.MediaFoundation;

// MfStressTest - Media Foundation reliability harness.
// See README.md for goals, design, CLI, and current findings.
//
// Build & run in Release:
//   dotnet run -c Release --project MfStressTest -- --duration 300 --threads 4 --soft-watchdog
//
// Exit codes (CI-stable):
//   0  = clean run, no AVs / hangs / exceptions
//   1  = managed exception escaped the run loop
//   2  = soft-watchdog mode + at least one hang was observed
//   3  = no encodable codecs detected on this machine (probe phase failed)
//   35 = Environment.FailFast (hard watchdog mode hit a hang) - .NET runtime
//   <other non-zero> = native crash (e.g. 0xC0000005 access violation) - OS-determined
const int ExitClean            = 0;
const int ExitManagedException = 1;
const int ExitHangsObserved    = 2;
const int ExitNoCodecs         = 3;

var options = Cli.ParseArgs(args);
// Resolve seed before printing the header so the chosen seed is in the run log
// from the very first line. Pass --seed N to repeat a specific seed's workload mix.
if (!options.SeedExplicit) options.Seed = Random.Shared.Next();
Cli.PrintHeader(options);
Cli.PrintLayoutAssertions();

MediaFoundationApi.Startup();
Console.WriteLine("MediaFoundationApi.Startup() OK");

var tempDir = options.TempDir ?? Path.Combine(Path.GetTempPath(), "MfStressTest_" + Guid.NewGuid().ToString("N").Substring(0, 8));
Directory.CreateDirectory(tempDir);
Console.WriteLine($"Temp dir: {tempDir}");

Watchdog.Start(options.WatchdogSeconds, options.ProcdumpPath, !options.SoftWatchdog, options.MaxDumps);

int exitCode = ExitClean;
var totalStopwatch = Stopwatch.StartNew();
try
{
    var workingCombos = Probe.Run(options, tempDir);
    if (workingCombos.Count == 0)
    {
        Console.WriteLine("WARN: no working encode combinations on this machine - skipping soak phase");
        exitCode = ExitNoCodecs;
    }
    else
    {
        // Time budget for `all`: ~70% soak, ~10% enum, ~10% abandon, ~10% sta.
        // For single-mode invocations the whole remaining budget goes to that mode.
        double Remaining() => options.Duration - totalStopwatch.Elapsed.TotalSeconds;

        switch (options.Mode)
        {
            case RunMode.Breadth:
                // Already done above as part of Probe.Run.
                break;
            case RunMode.Soak:
                if (Remaining() > 5) Soak.Run(options, tempDir, workingCombos, Remaining());
                break;
            case RunMode.Abandon:
                if (Remaining() > 5) Abandon.Run(options, workingCombos, Remaining());
                break;
            case RunMode.Sta:
                if (Remaining() > 5) StaSoak.Run(options, tempDir, workingCombos, Remaining());
                break;
            case RunMode.Enum:
                if (Remaining() > 1) EnumStress.Run(Remaining(), options.GcEvery);
                break;
            case RunMode.All:
                var totalBudget = Remaining();
                var soakBudget    = Math.Max(0, totalBudget * 0.70);
                var enumBudget    = Math.Max(0, Math.Min(30, totalBudget * 0.10));
                var abandonBudget = Math.Max(0, Math.Min(30, totalBudget * 0.10));
                var staBudget     = Math.Max(0, Math.Min(30, totalBudget * 0.10));
                if (soakBudget > 5)    Soak.Run(options, tempDir, workingCombos, soakBudget);
                if (enumBudget > 1)    EnumStress.Run(enumBudget, options.GcEvery);
                if (abandonBudget > 1) Abandon.Run(options, workingCombos, abandonBudget);
                if (staBudget > 5)     StaSoak.Run(options, tempDir, workingCombos, staBudget);
                break;
        }
    }

    var invalidPosCount = Volatile.Read(ref Counters.InvalidPositionCount);
    if (invalidPosCount > 0)
    {
        Console.WriteLine($"  (suppressed) MF_E_INVALID_POSITION (0xC00D36E5) on reposition: {invalidPosCount} occurrences");
    }

    var fileHangs = Volatile.Read(ref Watchdog.FileHangs);
    var streamHangs = Volatile.Read(ref Watchdog.StreamHangs);
    if (fileHangs + streamHangs > 0)
    {
        Console.WriteLine();
        Console.WriteLine($"=== HANG SUMMARY ===  file={fileHangs} (HIGH severity)  stream={streamHangs} (known ComStream issue)");
        lock (Watchdog.HangSummaries)
        {
            foreach (var s in Watchdog.HangSummaries) Console.WriteLine($"  {s}");
        }
        // We can only reach this point with hangs > 0 in soft-watchdog mode (hard mode
        // FailFasts before the summary). Treat as failure for CI; investigation runs
        // can ignore this exit code.
        if (exitCode == ExitClean) exitCode = ExitHangsObserved;
    }

    Console.WriteLine();
    Console.WriteLine($"DONE in {totalStopwatch.Elapsed.TotalSeconds:F1}s - no AV observed (hangs: file={fileHangs} stream={streamHangs}, exit={exitCode})");
}
catch (Exception ex)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("FATAL: managed exception escaped run loop");
    Console.Error.WriteLine(ex);
    exitCode = ExitManagedException;
}
finally
{
    try { MediaFoundationApi.Shutdown(); } catch { /* best-effort */ }
    if (!options.Keep)
    {
        try { Directory.Delete(tempDir, recursive: true); } catch { /* best-effort */ }
    }
    else
    {
        Console.WriteLine($"Keeping temp dir: {tempDir}");
    }
}
return exitCode;
