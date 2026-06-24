namespace NAudioConsoleTest.Shared.Testing;

/// <summary>
/// Command-line front door for the harness. Supports:
/// <list type="bullet">
///   <item><c>list</c> — print every registered test id and description.</item>
///   <item><c>describe &lt;id&gt;</c> — print a single test's parameters and defaults.</item>
///   <item><c>run &lt;id&gt; [--key=value ...] [--key value ...]</c> — run a single test non-interactively.</item>
///   <item><c>run-batch &lt;plan.json&gt; [--out=dir]</c> — execute a JSON batch plan, emit JSON + markdown report.</item>
///   <item><c>diagnose [--format=json|md] [--out=path]</c> — snapshot the host audio infrastructure.</item>
/// </list>
/// Exit codes: 0 = pass, 1 = fail, 2 = usage/parse error, 3 = unknown test.
/// </summary>
static class CliDispatcher
{
    public static bool TryHandle(string[] args, out int exitCode)
    {
        if (args.Length == 0)
        {
            exitCode = 0;
            return false; // fall through to interactive menu
        }

        exitCode = args[0] switch
        {
            "list" => List(),
            "describe" => Describe(args),
            "run" => Run(args),
            "run-batch" => BatchRunner.Run(args),
            "diagnose" => Diagnose(args),
            "--help" or "-h" or "help" => Help(),
            _ => Unknown(args[0]),
        };
        return true;
    }

    private static int Diagnose(string[] args)
    {
        var format = "json";
        string? outPath = null;
        for (int i = 1; i < args.Length; i++)
        {
            var a = args[i];
            if (a.StartsWith("--format=", StringComparison.Ordinal)) format = a[9..];
            else if (a.StartsWith("--out=", StringComparison.Ordinal)) outPath = a[6..];
            else
            {
                Console.Error.WriteLine($"Unknown argument: {a}");
                Console.Error.WriteLine("usage: diagnose [--format=json|md] [--out=path]");
                return 2;
            }
        }
        if (format is not ("json" or "md"))
        {
            Console.Error.WriteLine($"Unknown format: {format} (expected json or md)");
            return 2;
        }

        var diag = DiagnosticsCollector.Collect();
        var text = format == "json" ? DiagnosticsRenderer.ToJson(diag) : DiagnosticsRenderer.ToMarkdown(diag);
        if (outPath is null)
        {
            Console.WriteLine(text);
        }
        else
        {
            File.WriteAllText(outPath, text);
            Console.WriteLine($"Wrote {outPath}");
        }
        return 0;
    }

    private static int List()
    {
        foreach (var test in TestRegistry.All.OrderBy(t => t.Id))
        {
            Console.WriteLine($"{test.Id}\t{test.Description}");
        }
        return 0;
    }

    private static int Describe(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("usage: describe <id>");
            return 2;
        }
        if (!TestRegistry.TryGet(args[1], out var test))
        {
            Console.Error.WriteLine($"Unknown test: {args[1]}");
            return 3;
        }

        Console.WriteLine($"Id:          {test.Id}");
        Console.WriteLine($"Description: {test.Description}");
        if (test.MenuLocation is { } m)
        {
            var path = m.Group is null ? $"{m.Category} / {m.Label}" : $"{m.Category} / {m.Group} / {m.Label}";
            Console.WriteLine($"Menu:        {path}");
        }
        else
        {
            Console.WriteLine("Menu:        (CLI only)");
        }

        if (test.Parameters.Count == 0)
        {
            Console.WriteLine("Parameters:  (none)");
        }
        else
        {
            Console.WriteLine("Parameters:");
            foreach (var p in test.Parameters)
            {
                var req = p.Required ? "required" : $"default={p.Default ?? "null"}";
                var tag = p.CliOnly ? "  [cli-only]" : "";
                var help = p.Help is null ? "" : $"  -- {p.Help}";
                Console.WriteLine($"  --{p.Name}  ({p.Type.Name}, {req}){tag}{help}");
            }
        }
        return 0;
    }

    private static int Run(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("usage: run <id> [--key=value ...]");
            return 2;
        }
        if (!TestRegistry.TryGet(args[1], out var test))
        {
            Console.Error.WriteLine($"Unknown test: {args[1]}");
            return 3;
        }

        if (!TryParseParameters(test, args.AsSpan(2), out var values, out var err))
        {
            Console.Error.WriteLine(err);
            return 2;
        }

        var result = ConsoleTestRunner.InvokeWithCancellation(test, values, interactive: false);

        // Trailing one-line summary so CI logs can grep for it.
        var prefix = result.Outcome switch
        {
            TestOutcome.Pass => "PASS",
            TestOutcome.Fail => "FAIL",
            TestOutcome.Skipped => "SKIP",
            TestOutcome.NotAutomatable => "NA  ",
            _ => "??  ",
        };
        Console.WriteLine();
        Console.WriteLine($"{prefix} {test.Id}{(result.Message is null ? "" : $"  -- {result.Message}")}");
        if (result.Diagnostics is { } d)
        {
            foreach (var kv in d) Console.WriteLine($"     {kv.Key}={kv.Value}");
        }

        return result.Outcome == TestOutcome.Pass ? 0 : 1;
    }

    private static bool TryParseParameters(IConsoleTest test, ReadOnlySpan<string> argv,
        out IReadOnlyDictionary<string, object?> values, out string error)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var byName = test.Parameters.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < argv.Length; i++)
        {
            var a = argv[i];
            if (!a.StartsWith("--"))
            {
                values = dict; error = $"Unexpected argument '{a}' (parameters must be --name=value or --name value).";
                return false;
            }
            string name, raw;
            var eq = a.IndexOf('=');
            if (eq > 0)
            {
                name = a.Substring(2, eq - 2);
                raw = a[(eq + 1)..];
            }
            else
            {
                name = a[2..];
                if (i + 1 >= argv.Length)
                {
                    values = dict; error = $"Missing value for --{name}";
                    return false;
                }
                raw = argv[++i];
            }

            if (!byName.TryGetValue(name, out var p))
            {
                values = dict; error = $"Unknown parameter --{name} for {test.Id}";
                return false;
            }

            if (!TryConvert(raw, p.Type, out var converted))
            {
                values = dict; error = $"--{name}: cannot convert '{raw}' to {p.Type.Name}";
                return false;
            }
            dict[p.Name] = converted;
        }

        // Required check + defaults fill-in.
        foreach (var p in test.Parameters)
        {
            if (dict.ContainsKey(p.Name)) continue;
            if (p.Required)
            {
                values = dict; error = $"Missing required parameter --{p.Name}";
                return false;
            }
            dict[p.Name] = p.Default;
        }

        values = dict; error = "";
        return true;
    }

    internal static bool TryConvert(string raw, Type target, out object? value)
        => ParameterCoercion.TryConvert(raw, target, out value);

    private static int Help()
    {
        Console.WriteLine("NAudioConsoleTest — interactive audio test harness");
        Console.WriteLine();
        Console.WriteLine("Run with no arguments for the interactive menu, or:");
        Console.WriteLine("  list                            List every registered test");
        Console.WriteLine("  describe <id>                   Show a test's parameters");
        Console.WriteLine("  run <id> [--key=value ...]      Run a single test non-interactively");
        Console.WriteLine("  run-batch <plan.json> [--out=]  Execute a JSON batch plan");
        Console.WriteLine("  diagnose [--format=json|md] [--out=path]");
        Console.WriteLine("                                  Snapshot host audio infrastructure");
        return 0;
    }

    private static int Unknown(string arg)
    {
        Console.Error.WriteLine($"Unknown command: {arg}");
        Console.Error.WriteLine("Try: list | describe <id> | run <id> [--key=value ...]");
        return 2;
    }
}
