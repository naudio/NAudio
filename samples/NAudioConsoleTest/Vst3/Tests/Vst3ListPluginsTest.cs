using NAudio.Vst3;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Vst3.Tests;

/// <summary>
/// Enumerates installed VST 3 plug-ins via <see cref="Vst3PluginScanner"/> and loads each one
/// to verify the factory bindings succeed. In interactive mode renders a Spectre table; in CLI
/// mode prints one flat line per module so the output is greppable in batch logs.
/// </summary>
internal sealed class Vst3ListPluginsTest : IConsoleTest
{
    public string Id => "Vst3.ListPlugins";
    public string Description => "Enumerate installed VST 3 plug-ins and load each to verify factory bindings";
    public MenuPath? MenuLocation => new("VST 3", "List installed VST 3 plug-ins", Group: "Discovery", Order: 0);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("module", typeof(string), Required: false, Default: null,
            Help: "specific .vst3 path to inspect (default: scan all installed)"),
        new("kind", typeof(string), Required: false, Default: "all",
            Help: "filter to a plug-in kind: all | instrument | effect"),
    ];

    public TestResult Run(TestContext ctx)
    {
        ctx.TryGet<string>("module", out var modulePath);
        var kindFilter = ParseKindFilter(ctx.Get<string>("kind"), out var kindError);
        if (kindError is not null)
            return TestResult.Fail(kindError);

        IReadOnlyList<Vst3ModuleInfo> modules;
        if (!string.IsNullOrWhiteSpace(modulePath))
        {
            if (!File.Exists(modulePath))
                return TestResult.Fail($"Module not found: {modulePath}");
            modules = [new Vst3ModuleInfo(modulePath, Path.GetFileNameWithoutExtension(modulePath))];
        }
        else
        {
            modules = Vst3PluginScanner.EnumerateInstalled();
        }

        if (modules.Count == 0)
        {
            if (ctx.Interactive)
            {
                AnsiConsole.MarkupLine("[yellow]No VST 3 modules found. Search paths:[/]");
                foreach (var path in Vst3PluginScanner.DefaultSearchPaths)
                    AnsiConsole.MarkupLine($"  [dim]{Markup.Escape(path)}[/]");
            }
            else
            {
                Console.WriteLine("No VST 3 modules found. Default search paths:");
                foreach (var path in Vst3PluginScanner.DefaultSearchPaths)
                    Console.WriteLine($"  {path}");
            }
            return TestResult.Skipped("No VST 3 plug-ins installed");
        }

        var loaded = 0;
        var failed = 0;
        var matched = 0;

        if (ctx.Interactive)
        {
            var table = new Table().Border(TableBorder.Rounded)
                .AddColumn("Plug-in").AddColumn("Vendor").AddColumn("Kind").AddColumn("Classes");
            foreach (var entry in modules)
            {
                try
                {
                    using var module = Vst3Module.Load(entry.Path);
                    var info = module.GetFactoryInfo();
                    var classes = module.GetClasses();
                    loaded++;
                    if (kindFilter is not null && !ModuleMatchesKind(classes, kindFilter.Value))
                        continue;
                    matched++;
                    table.AddRow(
                        Markup.Escape(entry.Name), Markup.Escape(info.Vendor),
                        Markup.Escape(KindSummary(classes)), classes.Count.ToString());
                }
                catch (Exception ex)
                {
                    table.AddRow(
                        $"[dim]{Markup.Escape(entry.Name)}[/]",
                        $"[red]{Markup.Escape(ex.GetType().Name)}[/]",
                        "[red]—[/]",
                        $"[red]{Markup.Escape(ex.Message)}[/]");
                    failed++;
                }
            }
            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"\n[green]Loaded {loaded}[/] / [red]Failed {failed}[/]"
                + (kindFilter is null ? "" : $" / [blue]{matched} {kindFilter}[/]"));
        }
        else
        {
            Console.WriteLine($"Scanning {modules.Count} VST 3 module(s)...");
            Console.WriteLine();
            foreach (var entry in modules)
            {
                try
                {
                    using var module = Vst3Module.Load(entry.Path);
                    var info = module.GetFactoryInfo();
                    var classes = module.GetClasses();
                    loaded++;
                    if (kindFilter is not null && !ModuleMatchesKind(classes, kindFilter.Value))
                        continue;
                    matched++;
                    Console.WriteLine($"  OK    {entry.Name}  [{KindSummary(classes)}]");
                    Console.WriteLine($"          vendor : {info.Vendor}");
                    Console.WriteLine($"          classes: {classes.Count}");
                    foreach (var c in classes.Take(4))
                    {
                        var sub = string.IsNullOrEmpty(c.SubCategories) ? "" : $" ({c.SubCategories})";
                        Console.WriteLine($"            - {c.Name} [{c.Category}{sub}] -> {c.Kind}");
                    }
                    if (classes.Count > 4)
                        Console.WriteLine($"            ... and {classes.Count - 4} more");
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  FAIL  {entry.Name}  ({ex.GetType().Name}: {ex.Message})");
                    Console.WriteLine();
                    failed++;
                }
            }
        }

        var diagnostics = new Dictionary<string, string>
        {
            ["modulesFound"] = modules.Count.ToString(),
            ["loaded"] = loaded.ToString(),
            ["failed"] = failed.ToString(),
            ["kindFilter"] = kindFilter?.ToString() ?? "all",
            ["matched"] = matched.ToString(),
        };

        return failed == 0
            ? TestResult.Pass($"{loaded} module(s) loaded{(kindFilter is null ? "" : $", {matched} {kindFilter}")}", diagnostics)
            : TestResult.Fail($"{loaded} loaded, {failed} failed", diagnostics);
    }

    /// <summary>Parses the <c>kind</c> parameter into a filter; <c>null</c> means "all".</summary>
    private static Vst3PlugKind? ParseKindFilter(string? value, out string? error)
    {
        error = null;
        switch ((value ?? "all").Trim().ToLowerInvariant())
        {
            case "" or "all": return null;
            case "instrument" or "instruments" or "vsti": return Vst3PlugKind.Instrument;
            case "effect" or "effects" or "fx": return Vst3PlugKind.Effect;
            default:
                error = $"Unknown kind '{value}' (expected: all | instrument | effect)";
                return null;
        }
    }

    private static bool ModuleMatchesKind(IReadOnlyList<Vst3ClassInfo> classes, Vst3PlugKind kind)
    {
        foreach (var c in classes)
        {
            if (c.Kind == kind) return true;
        }
        return false;
    }

    /// <summary>Distinct kinds of the module's audio-module classes, e.g. "Instrument" or "Effect".</summary>
    private static string KindSummary(IReadOnlyList<Vst3ClassInfo> classes)
    {
        var kinds = classes.Where(c => c.IsAudioModule).Select(c => c.Kind).Distinct().ToList();
        return kinds.Count == 0 ? "—" : string.Join(", ", kinds);
    }
}
