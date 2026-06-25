namespace MfStressTest;

/// <summary>Run mode selected via <c>--mode</c>.</summary>
internal enum RunMode { Breadth, Soak, Enum, Abandon, Sta, All }

/// <summary>Whether an encoded clip lives on disk or in a MemoryStream.</summary>
internal enum Sink { File, Stream }

/// <summary>CLI options. Populated by <see cref="Cli.ParseArgs"/>.</summary>
internal sealed class Options
{
    public int Duration { get; set; } = 180;
    public RunMode Mode { get; set; } = RunMode.All;
    public int Threads { get; set; } = 1;
    public int GcEvery { get; set; } = 50;
    /// <summary>Seed for combo selection / per-worker RNGs. If <see cref="SeedExplicit"/>
    /// is false the seed is auto-generated at startup so each run uses a different
    /// workload mix; pass <c>--seed N</c> to repeat a specific run.</summary>
    public int Seed { get; set; }
    public bool SeedExplicit { get; set; }
    public string? TempDir { get; set; }
    public bool Verbose { get; set; }
    public bool Keep { get; set; }
    public int WatchdogSeconds { get; set; } = 10;
    public string? ProcdumpPath { get; set; }
    public bool SoftWatchdog { get; set; }
    public int MaxDumps { get; set; } = 3;
}

/// <summary>
/// Counters that accumulate across the run and get reported in the final summary.
/// Currently just suppressed-error counters; add more here as the harness evolves.
/// </summary>
internal static class Counters
{
    /// <summary>Number of <c>MF_E_INVALID_POSITION</c> (0xC00D36E5) errors swallowed
    /// during reposition. Common for very short clips and not interesting per-occurrence.</summary>
    public static long InvalidPositionCount;
}
