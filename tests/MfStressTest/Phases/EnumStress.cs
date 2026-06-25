using System.Diagnostics;

namespace MfStressTest.Phases;

/// <summary>
/// Tight loop over <c>EnumerateTransforms</c> + <c>GetOutputMediaTypes</c> with
/// the wrappers abandoned undisposed. Stresses the S1 dual-finalizer pattern in
/// <see cref="NAudio.MediaFoundation.MfActivate"/> and
/// <see cref="NAudio.MediaFoundation.MediaType"/> - the wrapper finalizer
/// releases the IntPtr while ComObject's finalizer independently releases the
/// RCW with no defined ordering.
///
/// If the S1 hypothesis is correct and an AV exists in the wrapper finalizer
/// chain, this phase is the most likely place to surface it.
/// </summary>
internal static class EnumStress
{
    public static void Run(double budgetSeconds, int gcEvery)
    {
        Console.WriteLine($"== Enum-stress phase ({budgetSeconds:F0}s) ==");
        var sw = Stopwatch.StartNew();
        long iter = 0, lastReportMs = 0;
        int sinceGc = 0;
        while (sw.Elapsed.TotalSeconds < budgetSeconds)
        {
            MfPrimitives.ChurnEnumerators();
            iter++;
            sinceGc++;
            if (gcEvery > 0 && sinceGc >= gcEvery)
            {
                sinceGc = 0;
                GC.Collect();
            }
            if (sw.ElapsedMilliseconds - lastReportMs > 1000)
            {
                lastReportMs = sw.ElapsedMilliseconds;
                Console.WriteLine($"  {sw.Elapsed.TotalSeconds,6:F1}s  enum iter={iter}");
            }
        }
        Console.WriteLine($"  enum: {iter} iterations in {sw.Elapsed.TotalSeconds:F1}s");
        Console.WriteLine();
    }
}
