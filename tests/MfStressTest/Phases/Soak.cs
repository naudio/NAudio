using System.Collections.Concurrent;
using System.Diagnostics;

namespace MfStressTest.Phases;

/// <summary>
/// The main soak loop: random sampling from the working set of combos with
/// randomised duration / frequency / reposition / resample / float-input
/// choices. Runs N parallel worker threads (`--threads`), each with its own
/// watchdog slot.
///
/// The driver thread on the side handles periodic GC and EnumerateTransforms
/// churn (~once every 50 iterations) - mostly to avoid having every worker
/// trip GC and serialize on it.
///
/// Workers that hang inside MF native code can't be aborted from managed code,
/// so we Join with a 2s timeout and let any stuck workers stay parked until
/// process exit.
/// </summary>
internal static class Soak
{
    public static void Run(Options o, string tempDir, List<Combo> combos, double budgetSeconds)
    {
        int threads = Math.Max(1, o.Threads);
        Console.WriteLine($"== Soak phase ({budgetSeconds:F0}s, threads={threads}) ==");

        var sw = Stopwatch.StartNew();
        long iter = 0;
        long lastReportMs = 0;
        var perCodec = new ConcurrentDictionary<string, long>();
        foreach (var name in combos.Select(c => c.Codec.Name).Distinct()) perCodec[name] = 0;

        void Worker(int slot)
        {
            // Per-worker RNG so multi-threaded runs are still deterministic per-seed.
            var rng = new Random(unchecked(o.Seed * 397 + slot));
            while (sw.Elapsed.TotalSeconds < budgetSeconds)
            {
                var combo = combos[rng.Next(combos.Count)];
                double duration = 1.0 + rng.NextDouble() * 3.0;
                double freq = 200 + rng.NextDouble() * 2000;
                bool useFloat = rng.Next(8) == 0;
                double repos = rng.Next(2) == 0 ? 0.33 : 0.0;
                bool resample = rng.Next(3) != 0;
                int targetRate = new[] { 16000, 22050, 32000, 44100, 48000 }[rng.Next(5)];
                long curIter = Interlocked.Increment(ref iter);

                try
                {
                    Watchdog.Beat(slot, $"soak[{slot}]:encode", (int)curIter, combo);
                    var encoded = MfPrimitives.EncodeOne(tempDir, combo, duration, freq, useFloat);
                    Watchdog.Beat(slot, $"soak[{slot}]:decode", (int)curIter, combo);
                    MfPrimitives.DecodeOne(encoded, combo, repos, resample, targetRate);
                    MfPrimitives.DisposeEncoded(encoded);
                }
                catch (NAudio.MediaFoundation.MediaFoundationException ex)
                {
                    if (ex.HResult == unchecked((int)0xC00D36E5))
                    {
                        // MF_E_INVALID_POSITION on reposition - already counted in MfPrimitives.
                        // Defensive double-tally if the exception escapes from elsewhere.
                        Interlocked.Increment(ref Counters.InvalidPositionCount);
                    }
                    else
                    {
                        Console.Error.WriteLine($"  MF exception in soak[{slot}] (combo={combo}, useFloat={useFloat}): 0x{ex.HResult:X8} {ex.Message}");
                    }
                }

                perCodec.AddOrUpdate(combo.Codec.Name, 1L, (_, v) => v + 1);
            }
        }

        var workers = new Thread[threads];
        for (int i = 0; i < threads; i++)
        {
            int slot = i;
            workers[i] = new Thread(() => Worker(slot)) { IsBackground = true, Name = $"MfSoakWorker-{slot}" };
            Watchdog.RegisterSlot(slot);
            workers[i].Start();
        }

        // Driver loop on the main thread: progress + GC + enumerator churn.
        while (sw.Elapsed.TotalSeconds < budgetSeconds)
        {
            Thread.Sleep(200);
            long curIter = Interlocked.Read(ref iter);
            if (o.GcEvery > 0 && curIter > 0 && curIter % o.GcEvery == 0)
            {
                // Intentionally not WaitForPendingFinalizers - finalizer thread should race.
                GC.Collect();
            }
            if (curIter > 0 && curIter % 50 == 0) MfPrimitives.ChurnEnumerators();
            if (sw.ElapsedMilliseconds - lastReportMs > 1000)
            {
                lastReportMs = sw.ElapsedMilliseconds;
                Console.WriteLine($"  {sw.Elapsed.TotalSeconds,6:F1}s  iter={curIter} (threads={threads})");
            }
        }

        // Workers stuck in MF native code never return. Join briefly and move on -
        // hung threads stay parked in the process until exit.
        int joined = 0;
        foreach (var t in workers) if (t.Join(2000)) joined++;
        if (joined < workers.Length)
        {
            Console.WriteLine($"  note: {workers.Length - joined} of {workers.Length} workers stuck in MF native code (background, ignored)");
        }
        Watchdog.UnregisterAllSoakSlots();

        Console.WriteLine($"  soak: {iter} iterations in {sw.Elapsed.TotalSeconds:F1}s");
        foreach (var (k, v) in perCodec.OrderBy(kv => kv.Key)) Console.WriteLine($"    {k,-4} {v}");
        Console.WriteLine();
    }
}
