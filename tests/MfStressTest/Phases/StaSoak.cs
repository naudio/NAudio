using System.Diagnostics;
using NAudio.Wave;

namespace MfStressTest.Phases;

/// <summary>
/// Soak loop on a single STA-apartment thread with
/// <c>MediaFoundationReaderSettings.SingleReaderObject = true</c> - the
/// documented STA configuration, the WinForms-event-handler shape.
///
/// MTA-only soak (the default) doesn't exercise the apartment-mismatch sharp
/// edges; this phase does. Different reader lifecycle, different threading
/// model.
/// </summary>
internal static class StaSoak
{
    public static void Run(Options o, string tempDir, List<Combo> combos, double budgetSeconds)
    {
        Console.WriteLine($"== STA phase ({budgetSeconds:F0}s) ==");
        var sw = Stopwatch.StartNew();
        long iter = 0;
        Exception? error = null;

        var staThread = new Thread(() =>
        {
            var rng = new Random(o.Seed ^ 0x53544100);
            try
            {
                while (sw.Elapsed.TotalSeconds < budgetSeconds)
                {
                    var combo = combos[rng.Next(combos.Count)];
                    double duration = 1.0 + rng.NextDouble() * 2.0;
                    double freq = 200 + rng.NextDouble() * 2000;
                    long curIter = Interlocked.Increment(ref iter);
                    try
                    {
                        Watchdog.Beat(0, "sta:encode", (int)curIter, combo);
                        var encoded = MfPrimitives.EncodeOne(tempDir, combo, duration, freq, useFloatInput: false);
                        Watchdog.Beat(0, "sta:decode", (int)curIter, combo);
                        DecodeOneSta(encoded);
                        MfPrimitives.DisposeEncoded(encoded);
                    }
                    catch (NAudio.MediaFoundation.MediaFoundationException ex)
                    {
                        if (ex.HResult == unchecked((int)0xC00D36E5))
                            Interlocked.Increment(ref Counters.InvalidPositionCount);
                        else
                            Console.Error.WriteLine($"  STA MF exception (combo={combo}): 0x{ex.HResult:X8} {ex.Message}");
                    }
                }
            }
            catch (Exception ex) { error = ex; }
        })
        { IsBackground = false, Name = "MfStaWorker" };

        staThread.SetApartmentState(ApartmentState.STA);
        staThread.Start();
        staThread.Join();

        if (error != null) throw error;
        Console.WriteLine($"  sta: {iter} iterations in {sw.Elapsed.TotalSeconds:F1}s");
        Console.WriteLine();
    }

    static void DecodeOneSta(EncodedClip clip)
    {
        // STA-mode decode: SingleReaderObject = true per the docs.
        var settings = new MediaFoundationReader.MediaFoundationReaderSettings { SingleReaderObject = true };
        using WaveStream reader = clip.Stream != null
            ? new StreamMediaFoundationReader(clip.Stream, settings)
            : new MediaFoundationReader(clip.File!, settings);
        Span<byte> buf = stackalloc byte[8192];
        while (true)
        {
            Watchdog.Beat();
            int got = reader.Read(buf);
            if (got == 0) break;
        }
    }
}
