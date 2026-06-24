using System.Runtime.InteropServices;
using NAudio.Wave;

namespace MfStressTest;

/// <summary>CLI parsing + start-up banner. Pure plumbing - phase logic lives elsewhere.</summary>
internal static class Cli
{
    public static Options ParseArgs(string[] args)
    {
        var o = new Options();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--duration": o.Duration = int.Parse(args[++i]); break;
                case "--mode":
                    o.Mode = args[++i].ToLowerInvariant() switch
                    {
                        "breadth" => RunMode.Breadth,
                        "soak"    => RunMode.Soak,
                        "enum"    => RunMode.Enum,
                        "abandon" => RunMode.Abandon,
                        "sta"     => RunMode.Sta,
                        "all"     => RunMode.All,
                        var s     => throw new ArgumentException($"unknown mode '{s}'"),
                    };
                    break;
                case "--threads":       o.Threads = int.Parse(args[++i]); break;
                case "--gc-every":      o.GcEvery = int.Parse(args[++i]); break;
                case "--seed":          o.Seed = int.Parse(args[++i]); o.SeedExplicit = true; break;
                case "--temp":          o.TempDir = args[++i]; break;
                case "--verbose":       o.Verbose = true; break;
                case "--keep":          o.Keep = true; break;
                case "--watchdog":      o.WatchdogSeconds = int.Parse(args[++i]); break;
                case "--procdump":      o.ProcdumpPath = args[++i]; break;
                case "--soft-watchdog": o.SoftWatchdog = true; break;
                case "--max-dumps":     o.MaxDumps = int.Parse(args[++i]); break;
                case "-h":
                case "--help":
                    PrintUsage();
                    Environment.Exit(0);
                    break;
                default:
                    throw new ArgumentException($"unknown arg '{args[i]}'");
            }
        }
        return o;
    }

    public static void PrintUsage()
    {
        Console.WriteLine("MfStressTest [options]");
        Console.WriteLine("  --duration N      total seconds (default 180)");
        Console.WriteLine("  --mode M          breadth | soak | enum | abandon | sta | all (default all)");
        Console.WriteLine("  --threads N       parallel soak workers (default 1; affects soak / all)");
        Console.WriteLine("  --gc-every N      GC.Collect every N iterations (default 50; 0 = off)");
        Console.WriteLine("  --seed N          seed for combo selection (default: auto-generate; pass to repeat a run)");
        Console.WriteLine("  --temp DIR        override temp dir for encoded clips");
        Console.WriteLine("  --verbose         log every iteration");
        Console.WriteLine("  --keep            do not delete temp dir on exit");
        Console.WriteLine("  --watchdog N      dump if no heartbeat for N seconds (default 10; 0 = off)");
        Console.WriteLine("  --procdump PATH   path to procdump.exe (default C:\\tools\\procdump.exe or $PROCDUMP_EXE)");
        Console.WriteLine("  --soft-watchdog   dump but don't FailFast on hang; mark slot inactive and keep running");
        Console.WriteLine("  --max-dumps N     max procdumps to capture per run (default 3; 0 = none)");
    }

    public static void PrintHeader(Options o)
    {
        Console.WriteLine("MfStressTest - Media Foundation reliability harness");
        Console.WriteLine($"  process: PID={Environment.ProcessId} bitness={(IntPtr.Size == 8 ? "x64" : "x86")} runtime={Environment.Version}");
        string seedTag = o.SeedExplicit ? "" : " (auto)";
        Console.WriteLine($"  config:  mode={o.Mode} duration={o.Duration}s threads={o.Threads} gcEvery={o.GcEvery} seed={o.Seed}{seedTag} verbose={o.Verbose} keep={o.Keep} watchdog={o.WatchdogSeconds}s");
#if DEBUG
        Console.WriteLine("  WARNING: built in DEBUG; AV is highly unlikely to repro. Rebuild with -c Release.");
#else
        Console.WriteLine("  build:   Release");
#endif
        Console.WriteLine();
    }

    public static void PrintLayoutAssertions()
    {
        int wfSize = Marshal.SizeOf<WaveFormat>();
        Console.WriteLine($"Marshal.SizeOf<WaveFormat> = {wfSize} (expect 18)");
        if (wfSize != 18) throw new InvalidOperationException($"WaveFormat layout size mismatch: {wfSize}");
    }
}
