using System.Globalization;
using NAudio.Vst3;
using NAudioConsoleTest.Shared.Testing;

namespace NAudioConsoleTest.Vst3.Tests;

/// <summary>
/// Diagnostic: save the plug-in's state and immediately load it back into the <em>same</em>
/// instance. Used to discriminate "plug-in's setState is symmetric in isolation" from "the crash
/// is specific to loading state into a fresh second instance".
/// </summary>
/// <remarks>
/// If this test passes for a plug-in whose <see cref="Vst3StateRoundtripTest"/> crashes, the gap
/// is fresh-instance specific — opens lifecycle / state-machine ordering hypotheses. If it
/// crashes here too, the failure is symmetric and reproduces with a single live instance, narrowing
/// the surface for further investigation (or pointing at a genuine plug-in defect).
/// </remarks>
internal sealed class Vst3SelfRoundtripTest : IConsoleTest
{
    public string Id => "Vst3.SelfRoundtrip";
    public string Description => "Save state, then immediately load it back into the same plug-in instance";
    public MenuPath? MenuLocation => new("VST 3", "State save/load on same instance", Group: "Effects", Order: 3);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("plugin", typeof(string), Required: true,
            Help: "plug-in name (case-insensitive substring match against installed modules)",
            ChoiceProvider: () => Vst3PluginScanner.EnumerateInstalled().Select(m => m.Name).ToList()),
        new("parameter", typeof(string), Required: false, Default: "",
            Help: "optional parameter name to nudge to a non-default value before saving (empty = leave defaults)"),
        new("value", typeof(double), Required: false, Default: 0.42,
            Help: "normalised value [0..1] for --parameter when supplied"),
        new("sampleRate", typeof(int), Required: false, Default: 44100,
            Help: "sample rate at setupProcessing time"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var pluginQuery = ctx.Get<string>("plugin");
        var paramName = ctx.Get<string>("parameter");
        var paramValue = ctx.Get<double>("value");
        var sampleRate = ctx.Get<int>("sampleRate");

        var moduleInfo = Vst3PluginScanner.EnumerateInstalled()
            .FirstOrDefault(m => m.Name.Contains(pluginQuery, StringComparison.OrdinalIgnoreCase));
        if (moduleInfo is null)
            return TestResult.Fail($"No installed plug-in matches '{pluginQuery}'");

        Console.WriteLine($"Plug-in : {moduleInfo.Name}");
        using var module = Vst3Module.Load(moduleInfo.Path);
        var audioModuleClass = module.GetClasses()
            .FirstOrDefault(c => c.Category == "Audio Module Class");
        if (audioModuleClass is null)
            return TestResult.Fail($"{moduleInfo.Name} has no Audio Module Class entry");

        using var plugin = module.CreatePlugin(audioModuleClass, sampleRate, maxBlockSize: 512);
        Console.WriteLine($"  i/o   : {plugin.InputChannelCount}-in / {plugin.OutputChannelCount}-out");
        Console.WriteLine($"  params: {plugin.Parameters.Count} ({(plugin.HasSeparateController ? "separate" : "shared")} controller)");

        if (!string.IsNullOrWhiteSpace(paramName))
        {
            var p = plugin.Parameters.FindByTitle(paramName);
            if (p is null)
            {
                var titles = string.Join(", ", plugin.Parameters.Take(20).Select(x => x.Title));
                return TestResult.Fail($"Parameter '{paramName}' not found. First parameters: {titles}");
            }
            p.NormalizedValue = paramValue;
            // Push the pending write through with a tiny throwaway block so the parameter actually
            // makes it into the component's internal state (Process() is what drains the queue).
            var dummyIn = new float[plugin.MaxBlockSize * plugin.InputChannelCount];
            var dummyOut = new float[plugin.MaxBlockSize * plugin.OutputChannelCount];
            plugin.Process(dummyIn, dummyOut, plugin.MaxBlockSize);
            Console.WriteLine($"  set   : {p.Title} = {paramValue:F3} ({p.FormatValue(paramValue)})");
        }

        Console.WriteLine("Saving state ...");
        var state = plugin.SaveState();
        Console.WriteLine($"  state size: {state.Length:N0} bytes");

        Console.WriteLine("Loading state back into the SAME instance ...");
        try
        {
            plugin.LoadState(state);
        }
        catch (Exception ex)
        {
            return TestResult.Fail(
                $"LoadState threw {ex.GetType().Name}: {ex.Message}",
                new Dictionary<string, string>
                {
                    ["plugin"] = moduleInfo.Name,
                    ["stateBytes"] = state.Length.ToString(CultureInfo.InvariantCulture),
                    ["exception"] = ex.GetType().FullName ?? ex.GetType().Name,
                });
        }
        Console.WriteLine("  LoadState returned cleanly.");

        // Sanity: round-trip the bytes once more — if SetState mutates internal state in a way that
        // makes a subsequent GetState diverge, we want to see it.
        var state2 = plugin.SaveState();
        var equal = state.AsSpan().SequenceEqual(state2);
        Console.WriteLine($"  post-load state size: {state2.Length:N0} bytes (byte-equal to pre-load: {equal})");

        return TestResult.Pass(
            $"Self round-trip clean on {moduleInfo.Name} ({state.Length} bytes, post-load equal={equal})",
            new Dictionary<string, string>
            {
                ["plugin"] = moduleInfo.Name,
                ["stateBytes"] = state.Length.ToString(CultureInfo.InvariantCulture),
                ["postLoadBytes"] = state2.Length.ToString(CultureInfo.InvariantCulture),
                ["postLoadEqual"] = equal.ToString(),
            });
    }
}
