using NAudio.Vst3;
using NAudioConsoleTest.Shared.Testing;

namespace NAudioConsoleTest.Vst3.Tests;

/// <summary>
/// Enumerates a VST 3 plug-in's <b>units and program lists</b> (its factory presets) via the new
/// <c>IUnitInfo</c> support — <see cref="Vst3Plugin.Units"/>, <see cref="Vst3Plugin.ProgramLists"/>,
/// <see cref="Vst3Plugin.ActiveProgramList"/>, <see cref="Vst3Plugin.CurrentProgram"/>. Headless
/// validation for Phase 10's program-list work: a real plug-in's program names print out, and the
/// program-change parameter (if any) is tied to the right list. The plug-in is resolved by
/// case-insensitive substring match against installed modules.
/// </summary>
sealed class Vst3ListProgramsTest : IConsoleTest
{
    public string Id => "Vst3.ListPrograms";
    public string Description => "List a VST 3 plug-in's units and program lists (factory presets)";
    public MenuPath? MenuLocation => new("VST 3", "List a plug-in's programs / presets", Group: "Presets", Order: 1);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("plugin", typeof(string), Required: true,
            Help: "plug-in name (case-insensitive substring match against installed modules)",
            ChoiceProvider: () => Vst3PluginScanner.EnumerateInstalled().Select(m => m.Name).ToList()),
        new("sampleRate", typeof(int), Required: false, Default: 44100, Help: "sample rate to instantiate at"),
        new("maxPrograms", typeof(int), Required: false, Default: 64, Help: "cap on program names printed per list"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var pluginQuery = ctx.Get<string>("plugin");
        var sampleRate = ctx.Get<int>("sampleRate");
        var maxPrograms = ctx.Get<int>("maxPrograms");

        var matches = Vst3PluginScanner.EnumerateInstalled()
            .Where(m => m.Name.Contains(pluginQuery, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (matches.Count == 0)
            return TestResult.Fail($"No installed plug-in matches '{pluginQuery}'");
        if (matches.Count > 1)
            return TestResult.Fail($"Multiple matches for '{pluginQuery}': {string.Join(", ", matches.Take(10).Select(m => m.Name))}");

        var moduleInfo = matches[0];
        Console.WriteLine($"Plug-in : {moduleInfo.Name}");
        Console.WriteLine($"  path  : {moduleInfo.Path}");

        using var module = Vst3Module.Load(moduleInfo.Path);
        var audioClass = module.GetClasses().FirstOrDefault(c => c.IsAudioModule);
        if (audioClass is null)
            return TestResult.Fail($"{moduleInfo.Name} exposes no audio-module class.");

        Console.WriteLine($"  class : {audioClass.Name}  [{audioClass.SubCategories}] -> {audioClass.Kind}");

        const int maxBlockSize = 512;
        using var plugin = module.CreatePlugin(audioClass, sampleRate, maxBlockSize);

        Console.WriteLine();
        Console.WriteLine($"Units        : {plugin.Units.Count}");
        foreach (var unit in plugin.Units)
        {
            var listNote = unit.ProgramListId >= 0 ? $", program-list {unit.ProgramListId}" : ", no program list";
            Console.WriteLine($"  [{unit.Id}] '{unit.Name}' (parent {unit.ParentId}{listNote})");
        }

        Console.WriteLine();
        Console.WriteLine($"Program lists: {plugin.ProgramLists.Count}");
        var totalPrograms = 0;
        foreach (var list in plugin.ProgramLists)
        {
            totalPrograms += list.Programs.Count;
            var active = ReferenceEquals(list, plugin.ActiveProgramList) ? "  <-- active" : "";
            Console.WriteLine($"  list {list.Id} '{list.Name}': {list.Programs.Count} programs{active}");
            var shown = Math.Min(list.Programs.Count, maxPrograms);
            for (var i = 0; i < shown; i++)
            {
                Console.WriteLine($"      {i,4}: {list.Programs[i]}");
            }
            if (shown < list.Programs.Count)
                Console.WriteLine($"      … {list.Programs.Count - shown} more");
        }

        Console.WriteLine();
        Console.WriteLine($"SupportsProgramChange : {plugin.SupportsProgramChange}");
        Console.WriteLine($"CurrentProgram        : {plugin.CurrentProgram}");

        var diagnostics = new Dictionary<string, string>
        {
            ["plugin"] = moduleInfo.Name,
            ["class"] = audioClass.Name,
            ["units"] = plugin.Units.Count.ToString(),
            ["programLists"] = plugin.ProgramLists.Count.ToString(),
            ["totalPrograms"] = totalPrograms.ToString(),
            ["supportsProgramChange"] = plugin.SupportsProgramChange.ToString(),
            ["currentProgram"] = plugin.CurrentProgram.ToString(),
        };

        if (plugin.ProgramLists.Count == 0)
            return TestResult.Pass($"{audioClass.Name} exposes no IUnitInfo program lists (common for plug-ins without factory programs)", diagnostics);

        return TestResult.Pass(
            $"{audioClass.Name}: {plugin.ProgramLists.Count} program list(s), {totalPrograms} program(s) total", diagnostics);
    }
}
