using System;

namespace AsioStressTest;

/// <summary>
/// Minimal CLI parser for the ASIO stress harness. Unknown flags are reported and abort the run
/// so a typo never silently changes the experiment.
/// </summary>
internal sealed class Options
{
    public string Driver { get; private set; } = "0";
    public int Iterations { get; private set; } = 100;
    public int PlayMs { get; private set; } = 250;
    public int SettleMs { get; private set; } = 150;
    public float Volume { get; private set; } = 0.03f;
    public bool ForceGc { get; private set; } = true;
    public bool ToggleVolume { get; private set; } = true;
    public bool KeepAlive { get; private set; }
    public bool ListOnly { get; private set; }

    public static Options? Parse(string[] args)
    {
        var o = new Options();
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            switch (a)
            {
                case "--driver": o.Driver = Next(args, ref i, a); break;
                case "--iterations": o.Iterations = int.Parse(Next(args, ref i, a)); break;
                case "--play-ms": o.PlayMs = int.Parse(Next(args, ref i, a)); break;
                case "--settle-ms": o.SettleMs = int.Parse(Next(args, ref i, a)); break;
                case "--volume": o.Volume = float.Parse(Next(args, ref i, a)); break;
                case "--no-gc": o.ForceGc = false; break;
                case "--no-toggle": o.ToggleVolume = false; break;
                case "--keep-alive": o.KeepAlive = true; break;
                case "--list": o.ListOnly = true; break;
                case "-h" or "--help":
                    PrintUsage();
                    return null;
                default:
                    Console.Error.WriteLine($"Unknown argument: {a}");
                    PrintUsage();
                    return null;
            }
        }
        return o;
    }

    private static string Next(string[] args, ref int i, string flag)
    {
        if (i + 1 >= args.Length) throw new ArgumentException($"Missing value for {flag}");
        return args[++i];
    }

    private static void PrintUsage()
    {
        Console.WriteLine(
            "Usage: AsioStressTest [options]\n" +
            "  --driver <name|index>  ASIO driver (default 0)\n" +
            "  --iterations N         open/play/stop/dispose cycles (default 100)\n" +
            "  --play-ms N            ms of audio per cycle (default 250)\n" +
            "  --settle-ms N          ms to wait after volume nudge (default 150)\n" +
            "  --volume f             gain 0..1 (default 0.03)\n" +
            "  --no-gc                skip the forced GC (control case)\n" +
            "  --no-toggle            skip the master-volume nudge\n" +
            "  --keep-alive           root disposed players so delegates survive GC (control case)\n" +
            "  --list                 list installed ASIO drivers and exit");
    }
}
