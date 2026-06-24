using System.Diagnostics;

namespace MfStressTest;

/// <summary>
/// Per-slot heartbeat watchdog. Each phase / worker beats its slot before any
/// potentially-blocking MF call; if a slot goes silent for longer than the
/// timeout, the watchdog captures a procdump and either FailFasts the process
/// (default) or marks the slot inactive and lets the run continue
/// (<c>--soft-watchdog</c>).
///
/// Severity is tagged by the hung combo's sink: file-backed hangs are HIGH
/// severity (no NAudio code in the byte-stream path); stream-backed hangs are
/// the known ComStream / IStream→IMFByteStream issue and lower priority.
///
/// Up to <c>--max-dumps</c> dumps captured per run (default 3); excess hangs
/// are still tagged and counted, just no procdump.
/// </summary>
internal static class Watchdog
{
    // Slot 0 is the "main / single-thread / one-off" slot used by all single-threaded
    // phases (breadth, enum, abandon, sta) and by .Beat() with no slot. Soak workers
    // claim slots 0..N-1 dynamically via RegisterSlot.
    const int MaxSlots = 64;
    static readonly long[] beatTicks = new long[MaxSlots];
    static readonly string[] phases = new string[MaxSlots];
    static readonly int[] iters = new int[MaxSlots];
    static readonly Combo?[] combos = new Combo?[MaxSlots];
    static readonly bool[] slotActive = new bool[MaxSlots];
    static int timeoutSec;
    static string procdumpExe = "";
    static string dumpDir = "";
    static Thread? thread;
    static bool failFastOnHang = true;
    static int maxDumps = 3;
    static int dumpsCaptured;

    public static int FileHangs;
    public static int StreamHangs;
    public static List<string> HangSummaries { get; } = new();

    public static void Start(int timeoutSeconds, string? procdumpOverride, bool failFast, int maxDumpsAllowed)
    {
        if (timeoutSeconds <= 0)
        {
            Console.WriteLine("Watchdog disabled");
            return;
        }
        timeoutSec = timeoutSeconds;
        failFastOnHang = failFast;
        maxDumps = maxDumpsAllowed;
        procdumpExe = procdumpOverride
            ?? Environment.GetEnvironmentVariable("PROCDUMP_EXE")
            ?? @"C:\tools\procdump.exe";
        dumpDir = Path.Combine(Path.GetTempPath(), "MfStressTest_dumps");
        Directory.CreateDirectory(dumpDir);
        for (int i = 0; i < phases.Length; i++) phases[i] = "(idle)";
        slotActive[0] = true;
        Volatile.Write(ref beatTicks[0], Stopwatch.GetTimestamp());
        var procdumpStatus = File.Exists(procdumpExe)
            ? $"procdump={procdumpExe}"
            : $"procdump=NOT FOUND at {procdumpExe} (dumps will be skipped; install procdump.exe or set --procdump / $PROCDUMP_EXE)";
        Console.WriteLine($"Watchdog: {timeoutSec}s, {procdumpStatus}, dumps -> {dumpDir}");
        thread = new Thread(Run) { IsBackground = true, Name = "MfStressTest-Watchdog" };
        thread.Start();
    }

    public static void RegisterSlot(int slot)
    {
        if (slot < 0 || slot >= MaxSlots) return;
        phases[slot] = "(starting)";
        Volatile.Write(ref beatTicks[slot], Stopwatch.GetTimestamp());
        Volatile.Write(ref slotActive[slot], true);
    }

    /// <summary>Mark all soak-worker slots inactive (slot 0 stays for other phases).</summary>
    public static void UnregisterAllSoakSlots()
    {
        for (int i = 1; i < MaxSlots; i++) Volatile.Write(ref slotActive[i], false);
    }

    /// <summary>Update slot's phase / iter / combo and refresh its heartbeat.</summary>
    public static void Beat(int slot, string newPhase, int newIter, Combo? newCombo)
    {
        if (slot < 0 || slot >= MaxSlots) return;
        phases[slot] = newPhase;
        iters[slot] = newIter;
        combos[slot] = newCombo;
        Volatile.Write(ref beatTicks[slot], Stopwatch.GetTimestamp());
    }

    /// <summary>Convenience: beat slot 0 (single-thread phases).</summary>
    public static void Beat(string newPhase, int newIter, Combo? newCombo) => Beat(0, newPhase, newIter, newCombo);

    /// <summary>
    /// Refresh all currently-active slots' heartbeats. Used inside drain loops
    /// where the calling code doesn't know its slot. Cheap (a few volatile writes).
    /// </summary>
    public static void Beat()
    {
        long now = Stopwatch.GetTimestamp();
        for (int i = 0; i < MaxSlots; i++)
        {
            if (Volatile.Read(ref slotActive[i])) Volatile.Write(ref beatTicks[i], now);
        }
    }

    static void Run()
    {
        while (true)
        {
            Thread.Sleep(1000);
            int worstSlot = -1;
            double worstIdle = 0;
            for (int i = 0; i < MaxSlots; i++)
            {
                if (!Volatile.Read(ref slotActive[i])) continue;
                long last = Volatile.Read(ref beatTicks[i]);
                double idle = Stopwatch.GetElapsedTime(last).TotalSeconds;
                if (idle > worstIdle) { worstIdle = idle; worstSlot = i; }
            }
            if (worstSlot >= 0 && worstIdle > timeoutSec)
            {
                TriggerHang(worstSlot, worstIdle);
                if (failFastOnHang) return;
                // Soft mode: TriggerHang marks the slot inactive so we don't fire
                // repeatedly on the same one. Continue monitoring other slots.
            }
        }
    }

    static void TriggerHang(int slot, double idleSeconds)
    {
        var combo = combos[slot];
        var sink = combo?.Sink ?? Sink.File;
        var phase = phases[slot];
        var iter = iters[slot];
        var severity = sink == Sink.Stream
            ? "[STREAM-BASED — KNOWN ISSUE: ComStream / IStream→IMFByteStream]"
            : "[FILE-BASED — HIGH SEVERITY: pure MF source-reader hang]";

        Console.Error.WriteLine();
        Console.Error.WriteLine($"!!! WATCHDOG HANG {severity}");
        Console.Error.WriteLine($"    no heartbeat for {idleSeconds:F1}s on slot={slot} (timeout={timeoutSec}s)");
        Console.Error.WriteLine($"    phase={phase} iter={iter} combo={combo?.ToString() ?? "(none)"}");
        Console.Error.WriteLine($"    pid={Environment.ProcessId}");

        int alreadyDumped = Interlocked.Increment(ref dumpsCaptured) - 1;
        if (!File.Exists(procdumpExe))
        {
            Console.Error.WriteLine($"    procdump not at {procdumpExe} - skipping dump");
        }
        else if (alreadyDumped >= maxDumps)
        {
            Console.Error.WriteLine($"    dump cap reached ({maxDumps}) - skipping dump (use --max-dumps to raise)");
        }
        else
        {
            var sevTag = sink == Sink.Stream ? "stream" : "file";
            var dumpPath = Path.Combine(dumpDir, $"hang_{sevTag}_{DateTime.Now:yyyyMMdd_HHmmss}_iter{iter}.dmp");
            Console.Error.WriteLine($"    capturing dump -> {dumpPath} ({alreadyDumped + 1}/{maxDumps})");
            try
            {
                var psi = new ProcessStartInfo(procdumpExe,
                    $"-accepteula -ma {Environment.ProcessId} \"{dumpPath}\"")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                using var p = Process.Start(psi)!;
                if (!p.WaitForExit(60_000))
                {
                    try { p.Kill(); } catch { }
                    Console.Error.WriteLine("    procdump timed out after 60s");
                }
                else
                {
                    var sizeStr = File.Exists(dumpPath) ? $"{new FileInfo(dumpPath).Length / 1024 / 1024} MB" : "(missing)";
                    Console.Error.WriteLine($"    procdump exit={p.ExitCode} dump-size={sizeStr}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"    procdump failed: {ex.Message}");
            }
        }

        if (sink == Sink.Stream) Interlocked.Increment(ref StreamHangs);
        else Interlocked.Increment(ref FileHangs);
        lock (HangSummaries) HangSummaries.Add($"{severity} slot={slot} {phase} iter={iter} combo={combo}");

        if (failFastOnHang)
        {
            Environment.FailFast($"MfStressTest watchdog: {severity} {phase} iter={iter} combo={combo}");
        }
        else
        {
            // Soft mode: the hung thread will stay hung in MF land (we can't safely
            // kill it). Stop monitoring it so we don't fire repeatedly on the same
            // slot. Other slots / phases continue.
            Volatile.Write(ref slotActive[slot], false);
            Console.Error.WriteLine($"    soft mode: slot {slot} marked inactive; continuing run");
        }
    }
}
