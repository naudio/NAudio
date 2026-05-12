using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NAudioConsoleTest.Shared.Testing;

/// <summary>
/// Runs a JSON batch plan: looks up each entry by id, merges plan params with declared defaults,
/// executes non-interactively with a per-plan timeout and Ctrl+C cancellation, and emits a
/// structured JSON report plus a human-readable markdown summary. Exit code = number of
/// non-Pass results (capped at 255).
/// </summary>
public static class BatchRunner
{
    public static int Run(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("usage: run-batch <plan.json> [--out=path]");
            return 2;
        }
        var planPath = args[1];
        string? outDir = null;
        for (int i = 2; i < args.Length; i++)
        {
            var a = args[i];
            if (a.StartsWith("--out=", StringComparison.Ordinal)) outDir = a[6..];
            else
            {
                Console.Error.WriteLine($"Unknown argument: {a}");
                return 2;
            }
        }

        if (!File.Exists(planPath))
        {
            Console.Error.WriteLine($"Plan file not found: {planPath}");
            return 2;
        }

        BatchPlan plan;
        try
        {
            plan = JsonSerializer.Deserialize<BatchPlan>(File.ReadAllText(planPath), PlanJsonOptions)
                ?? throw new InvalidOperationException("Plan deserialised to null");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to parse plan: {ex.Message}");
            return 2;
        }
        if (plan.Tests is null || plan.Tests.Count == 0)
        {
            Console.Error.WriteLine("Plan has no tests");
            return 2;
        }

        outDir ??= Path.Combine("Reports", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(outDir);

        var planName = plan.Name ?? Path.GetFileNameWithoutExtension(planPath);
        var stopOnFail = plan.StopOnFail ?? false;
        var defaultTimeout = plan.TimeoutSeconds is > 0 ? TimeSpan.FromSeconds(plan.TimeoutSeconds.Value) : TimeSpan.FromMinutes(5);

        Console.WriteLine($"Batch: {planName}");
        Console.WriteLine($"Plan:  {planPath}");
        Console.WriteLine($"Out:   {outDir}");
        Console.WriteLine($"Tests: {plan.Tests.Count}  (stopOnFail={stopOnFail}, timeout={defaultTimeout.TotalSeconds:F0}s)");
        Console.WriteLine();

        var host = DiagnosticsCollector.Collect();
        using var batchCts = new CancellationTokenSource();
        void OnCancel(object? _, ConsoleCancelEventArgs e)
        {
            if (batchCts.IsCancellationRequested) return;
            e.Cancel = true;
            batchCts.Cancel();
            Console.Error.WriteLine("\nCtrl+C received — finishing current test and stopping batch.");
        }
        Console.CancelKeyPress += OnCancel;

        var results = new List<BatchResult>(plan.Tests.Count);
        var startedUtc = DateTime.UtcNow;
        var batchSw = Stopwatch.StartNew();

        try
        {
            foreach (var entry in plan.Tests)
            {
                if (batchCts.IsCancellationRequested) break;

                var elapsed = RunEntry(entry, defaultTimeout, batchCts.Token, out var result);
                results.Add(result with { ElapsedMs = elapsed });

                if (stopOnFail && result.Outcome != nameof(TestOutcome.Pass)) break;
            }
        }
        finally
        {
            Console.CancelKeyPress -= OnCancel;
        }

        batchSw.Stop();
        var completedUtc = DateTime.UtcNow;

        var report = new BatchReport(
            Name: planName,
            PlanFile: Path.GetFullPath(planPath),
            StartedUtc: startedUtc.ToString("o"),
            CompletedUtc: completedUtc.ToString("o"),
            ElapsedMs: batchSw.ElapsedMilliseconds,
            Host: host,
            Results: results);

        var jsonPath = Path.Combine(outDir, "summary.json");
        var mdPath = Path.Combine(outDir, "summary.md");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, ReportJsonOptions));
        File.WriteAllText(mdPath, RenderMarkdown(report));

        Console.WriteLine();
        Console.WriteLine($"Wrote {jsonPath}");
        Console.WriteLine($"Wrote {mdPath}");

        var nonPass = results.Count(r => r.Outcome != nameof(TestOutcome.Pass));
        var passed = results.Count - nonPass;
        Console.WriteLine($"\n{passed}/{results.Count} passed  ({batchSw.Elapsed.TotalSeconds:F1}s)");
        return Math.Min(nonPass, 255);
    }

    private static long RunEntry(BatchEntry entry, TimeSpan defaultTimeout,
        CancellationToken batchToken, out BatchResult result)
    {
        var sw = Stopwatch.StartNew();
        if (!TestRegistry.TryGet(entry.Id, out var test))
        {
            sw.Stop();
            Console.WriteLine($"[??  ] {entry.Id}  -- unknown test id");
            result = new BatchResult(entry.Id, entry.Params ?? new Dictionary<string, JsonElement>(),
                Outcome: "Fail", Message: $"Unknown test id: {entry.Id}", ElapsedMs: sw.ElapsedMilliseconds,
                Diagnostics: null);
            return sw.ElapsedMilliseconds;
        }

        if (!TryBuildValues(test, entry.Params, out var values, out var err))
        {
            sw.Stop();
            Console.WriteLine($"[FAIL] {entry.Id}  -- {err}");
            result = new BatchResult(entry.Id, entry.Params ?? new Dictionary<string, JsonElement>(),
                Outcome: "Fail", Message: err, ElapsedMs: sw.ElapsedMilliseconds, Diagnostics: null);
            return sw.ElapsedMilliseconds;
        }

        var timeout = entry.TimeoutSeconds is > 0 ? TimeSpan.FromSeconds(entry.TimeoutSeconds.Value) : defaultTimeout;
        using var testCts = CancellationTokenSource.CreateLinkedTokenSource(batchToken);
        testCts.CancelAfter(timeout);

        var ctx = new TestContext(values, interactive: false, testCts.Token);
        var tr = ConsoleTestRunner.Invoke(test, ctx);
        sw.Stop();

        var prefix = tr.Outcome switch
        {
            TestOutcome.Pass => "PASS",
            TestOutcome.Fail => "FAIL",
            TestOutcome.Skipped => "SKIP",
            TestOutcome.NotAutomatable => "NA  ",
            _ => "??  ",
        };
        Console.WriteLine($"[{prefix}] {entry.Id}  ({sw.ElapsedMilliseconds} ms)" +
            (string.IsNullOrEmpty(tr.Message) ? "" : $"  -- {tr.Message}"));

        result = new BatchResult(
            Id: entry.Id,
            Params: entry.Params ?? new Dictionary<string, JsonElement>(),
            Outcome: tr.Outcome.ToString(),
            Message: tr.Message,
            ElapsedMs: sw.ElapsedMilliseconds,
            Diagnostics: tr.Diagnostics);
        return sw.ElapsedMilliseconds;
    }

    private static bool TryBuildValues(IConsoleTest test,
        IReadOnlyDictionary<string, JsonElement>? planParams,
        out IReadOnlyDictionary<string, object?> values, out string error)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var byName = test.Parameters.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        if (planParams is not null)
        {
            foreach (var (key, element) in planParams)
            {
                if (!byName.TryGetValue(key, out var p))
                {
                    values = dict; error = $"Unknown parameter '{key}' for {test.Id}";
                    return false;
                }
                // JSON strings come through as-is; numbers/bools/etc. round-trip via raw text so
                // they reuse ParameterCoercion (handles TimeSpan strings like "5s" the same way
                // the CLI does).
                var raw = element.ValueKind == JsonValueKind.String
                    ? element.GetString() ?? ""
                    : element.GetRawText();
                if (!ParameterCoercion.TryConvert(raw, p.Type, out var converted))
                {
                    values = dict; error = $"--{key}: cannot convert '{raw}' to {p.Type.Name}";
                    return false;
                }
                dict[p.Name] = converted;
            }
        }

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

    private static string RenderMarkdown(BatchReport r)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Batch: {r.Name}");
        sb.AppendLine();
        sb.AppendLine($"- Plan: `{r.PlanFile}`");
        sb.AppendLine($"- Started: {r.StartedUtc}");
        sb.AppendLine($"- Completed: {r.CompletedUtc} ({r.ElapsedMs / 1000.0:F1}s elapsed)");
        sb.AppendLine();

        var passed = r.Results.Count(x => x.Outcome == nameof(TestOutcome.Pass));
        sb.AppendLine($"**{passed}/{r.Results.Count} passed**");
        sb.AppendLine();

        sb.AppendLine("## Host");
        sb.AppendLine($"- OS: {r.Host.Os.OsDescription} ({r.Host.Os.Architecture})");
        sb.AppendLine($"- Runtime: {r.Host.Os.RuntimeVersion}");
        sb.AppendLine($"- Machine: {r.Host.Os.MachineName}");
        if (r.Host.Asio.Drivers.Count > 0) sb.AppendLine($"- ASIO: {string.Join(", ", r.Host.Asio.Drivers)}");
        if (r.Host.Wasapi.Endpoints.Count > 0)
            sb.AppendLine($"- WASAPI: {r.Host.Wasapi.Endpoints.Count} endpoints");
        sb.AppendLine();

        sb.AppendLine("## Results");
        sb.AppendLine();
        sb.AppendLine("| # | Test | Outcome | Elapsed | Message |");
        sb.AppendLine("|---|------|---------|---------|---------|");
        for (int i = 0; i < r.Results.Count; i++)
        {
            var res = r.Results[i];
            var msg = (res.Message ?? "").Replace('|', '/').Replace('\n', ' ');
            sb.AppendLine($"| {i + 1} | {res.Id} | {res.Outcome} | {res.ElapsedMs} ms | {msg} |");
        }
        sb.AppendLine();

        var failed = r.Results.Where(x => x.Outcome != nameof(TestOutcome.Pass)).ToList();
        if (failed.Count > 0)
        {
            sb.AppendLine("## Non-pass details");
            foreach (var f in failed)
            {
                sb.AppendLine();
                sb.AppendLine($"### {f.Id} — {f.Outcome}");
                if (!string.IsNullOrEmpty(f.Message)) sb.AppendLine($"_{f.Message}_");
                if (f.Params.Count > 0)
                {
                    sb.AppendLine("Params:");
                    foreach (var kv in f.Params)
                        sb.AppendLine($"- {kv.Key} = {(kv.Value.ValueKind == JsonValueKind.String ? kv.Value.GetString() : kv.Value.GetRawText())}");
                }
                if (f.Diagnostics is { Count: > 0 } d)
                {
                    sb.AppendLine("Diagnostics:");
                    foreach (var kv in d) sb.AppendLine($"- {kv.Key} = {kv.Value}");
                }
            }
        }

        return sb.ToString();
    }

    private static readonly JsonSerializerOptions PlanJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly JsonSerializerOptions ReportJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}

public sealed record BatchPlan(
    string? Name,
    bool? StopOnFail,
    int? TimeoutSeconds,
    IReadOnlyList<BatchEntry> Tests);

public sealed record BatchEntry(
    string Id,
    [property: JsonPropertyName("params")] IReadOnlyDictionary<string, JsonElement>? Params = null,
    int? TimeoutSeconds = null);

public sealed record BatchReport(
    string Name,
    string PlanFile,
    string StartedUtc,
    string CompletedUtc,
    long ElapsedMs,
    HostDiagnostics Host,
    IReadOnlyList<BatchResult> Results);

public sealed record BatchResult(
    string Id,
    IReadOnlyDictionary<string, JsonElement> Params,
    string Outcome,
    string? Message,
    long ElapsedMs,
    IReadOnlyDictionary<string, string>? Diagnostics);
