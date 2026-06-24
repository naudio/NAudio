using System.Diagnostics;
using NAudio.MediaFoundation;
using NAudio.Wave;

namespace MfStressTest.Phases;

/// <summary>
/// Encode-abandon stress: construct <see cref="MediaFoundationEncoder"/>, do
/// nothing with it, drop the reference. The MediaType / MfSinkWriter / sample
/// / buffer chain is then released by finalizers in undefined order (S1 dual-
/// finalizer pattern).
///
/// Periodic GC drives the finalizer thread to actually run while we're
/// constructing more. If finalizer-order matters, this phase is where it'd
/// surface - either as an AV or a managed exception.
/// </summary>
internal static class Abandon
{
    public static void Run(Options o, List<Combo> combos, double budgetSeconds)
    {
        Console.WriteLine($"== Abandon phase ({budgetSeconds:F0}s) ==");
        var rng = new Random(o.Seed ^ 0x4d4f4445);
        var sw = Stopwatch.StartNew();
        long iter = 0, lastReportMs = 0;
        int sinceGc = 0;

        // Filter to codecs the simple SelectMediaType path supports.
        var helperCodecs = combos.Where(c => c.Codec.Name is "MP3" or "WMA" or "AAC")
                                 .Select(c => c.Codec).Distinct().ToList();
        if (helperCodecs.Count == 0) { Console.WriteLine("  (no MP3/WMA/AAC available - skipping)"); return; }

        while (sw.Elapsed.TotalSeconds < budgetSeconds)
        {
            var codec = helperCodecs[rng.Next(helperCodecs.Count)];
            var fmt = new WaveFormat(44100, 16, 2);
            Watchdog.Beat(0, $"abandon:{codec.Name}", (int)iter, null);
            try
            {
                var mt = MediaFoundationEncoder.SelectMediaType(codec.Subtype, fmt, 128_000);
                if (mt == null) continue;
                // Construct and abandon. NO Dispose. The encoder + its underlying
                // sink writer / sample / buffer chain are now eligible for finalization,
                // and the next GC.Collect() will run the finalizers in undefined order.
                _ = new MediaFoundationEncoder(mt);
            }
            catch (NAudio.MediaFoundation.MediaFoundationException ex)
            {
                Console.Error.WriteLine($"  abandon MF exception ({codec.Name}): 0x{ex.HResult:X8}");
            }

            iter++;
            sinceGc++;
            if (o.GcEvery > 0 && sinceGc >= o.GcEvery)
            {
                sinceGc = 0;
                GC.Collect();
            }
            if (sw.ElapsedMilliseconds - lastReportMs > 1000)
            {
                lastReportMs = sw.ElapsedMilliseconds;
                Console.WriteLine($"  {sw.Elapsed.TotalSeconds,6:F1}s  abandon iter={iter}");
            }
        }
        Console.WriteLine($"  abandon: {iter} iterations in {sw.Elapsed.TotalSeconds:F1}s");
        Console.WriteLine();
    }
}
