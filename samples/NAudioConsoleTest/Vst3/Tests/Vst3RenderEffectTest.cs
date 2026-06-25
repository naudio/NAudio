using System.Globalization;
using NAudio.Vst3;
using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using Spectre.Console;

namespace NAudioConsoleTest.Vst3.Tests;

/// <summary>
/// Feeds an input WAV through a chosen VST 3 effect plug-in and writes the result to another
/// WAV. The plug-in is resolved by case-insensitive substring match against the installed VST 3
/// modules; mono inputs are upmixed to stereo automatically. Output peak / head-RMS / tail-RMS
/// are emitted as diagnostics so the numbers can be diffed between runs.
/// </summary>
internal sealed class Vst3RenderEffectTest : IConsoleTest
{
    public string Id => "Vst3.RenderEffect";
    public string Description => "Render a WAV through a VST 3 effect plug-in";
    public MenuPath? MenuLocation => new("VST 3", "Render WAV through a VST 3 effect", Group: "Effects", Order: 0);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("plugin", typeof(string), Required: true,
            Help: "plug-in name (case-insensitive substring match against installed modules)",
            ChoiceProvider: () => Vst3PluginScanner.EnumerateInstalled().Select(m => m.Name).ToList()),
        new("input", typeof(string), Required: true, Help: "input WAV path", IsFilePath: true, FileCategory: "audio"),
        new("output", typeof(string), Required: true, Help: "output WAV path"),
        new("tailSeconds", typeof(double), Required: false, Default: -1.0,
            Help: "safety cap on the tail-render phase (seconds); -1 = use the provider default (30 s). Tail is detected automatically; this only bounds the wait for plug-ins that never settle"),
        new("params", typeof(string), Required: false, Default: "",
            Help: "semicolon-separated Title=normalizedValue list, e.g. \"Mix=0.7;Density=0.3\""),
        new("loadState", typeof(string), Required: false, Default: "",
            Help: "path to a state file produced by --saveState; applied before --params"),
        new("saveState", typeof(string), Required: false, Default: "",
            Help: "if set, write the post-render plug-in state to this path"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var pluginQuery = ctx.Get<string>("plugin");
        var inputPath = ctx.Get<string>("input");
        var outputPath = ctx.Get<string>("output");
        var explicitTail = ctx.Get<double>("tailSeconds");
        var paramOverrides = ctx.Get<string>("params");
        var loadStatePath = ctx.Get<string>("loadState");
        var saveStatePath = ctx.Get<string>("saveState");

        if (!File.Exists(inputPath))
            return TestResult.Fail($"Input not found: {inputPath}");

        var moduleInfo = ResolveModule(pluginQuery, out var resolveError);
        if (moduleInfo is null)
            return TestResult.Fail(resolveError ?? "Plug-in resolution failed");

        Console.WriteLine($"Plug-in : {moduleInfo.Name}");
        Console.WriteLine($"  path  : {moduleInfo.Path}");

        using var module = Vst3Module.Load(moduleInfo.Path);
        var audioModuleClass = module.GetClasses()
            .FirstOrDefault(c => c.Category == "Audio Module Class");
        if (audioModuleClass is null)
            return TestResult.Fail($"{moduleInfo.Name} has no Audio Module Class entry — not an audio effect / instrument");

        Console.WriteLine($"  class : {audioModuleClass.Name}  [{audioModuleClass.SubCategories}]");

        using var reader = new WaveFileReader(inputPath);
        Console.WriteLine($"Input   : {inputPath}");
        Console.WriteLine($"  format: {reader.WaveFormat.SampleRate} Hz, {reader.WaveFormat.Channels} ch, {reader.TotalTime}");

        var sampleSource = reader.ToSampleProvider();

        const int maxBlockSize = 512;
        using var plugin = module.CreatePlugin(audioModuleClass, reader.WaveFormat.SampleRate, maxBlockSize);
        Console.WriteLine($"  i/o   : {plugin.InputChannelCount}-in / {plugin.OutputChannelCount}-out");
        var source = AdaptChannels(sampleSource, plugin.InputChannelCount);
        Console.WriteLine($"  latency: {plugin.LatencySamples} samples, tail: {DescribeTail(plugin.TailSamples)}");
        Console.WriteLine($"  params : {plugin.Parameters.Count} ({(plugin.HasSeparateController ? "separate" : "shared")} controller)");

        if (!string.IsNullOrEmpty(loadStatePath))
        {
            if (!File.Exists(loadStatePath))
                return TestResult.Fail($"loadState file not found: {loadStatePath}");
            plugin.LoadState(File.ReadAllBytes(loadStatePath));
            Console.WriteLine($"  loaded state: {loadStatePath}");
        }

        var appliedParams = ApplyParamOverrides(plugin, paramOverrides, out var paramError);
        if (paramError is not null)
            return TestResult.Fail(paramError);
        foreach (var (name, value, display) in appliedParams)
            Console.WriteLine($"  param  : {name} -> {value:F3} ({display})");

        // Vst3EffectSampleProvider renders the tail automatically (RenderTail = true by default):
        // after the source returns 0 it feeds the plug-in zero-input blocks and stops when the
        // output's RMS settles. --tailSeconds caps the wait for plug-ins that never go silent.
        var fx = explicitTail >= 0
            ? new Vst3EffectSampleProvider(source, plugin) { MaxTailDuration = TimeSpan.FromSeconds(explicitTail) }
            : new Vst3EffectSampleProvider(source, plugin);
        WaveFileWriter.CreateWaveFile(outputPath, fx.ToWaveProvider());

        if (!string.IsNullOrEmpty(saveStatePath))
        {
            File.WriteAllBytes(saveStatePath, plugin.SaveState());
            Console.WriteLine($"  saved state: {saveStatePath}");
        }

        var outInfo = new FileInfo(outputPath);
        var (duration, peak, headRms, tailRms) = AnalyseOutput(outputPath);
        Console.WriteLine($"Output  : {outputPath}");
        Console.WriteLine($"  size  : {outInfo.Length:N0} bytes");
        Console.WriteLine($"  length: {duration}");
        Console.WriteLine($"  peak  : {Db(peak)} dBFS");
        Console.WriteLine($"  head 200ms RMS: {Db(headRms)} dBFS");
        Console.WriteLine($"  tail 200ms RMS: {Db(tailRms)} dBFS");

        return TestResult.Pass(
            $"Rendered {duration} via {moduleInfo.Name}",
            new Dictionary<string, string>
            {
                ["plugin"] = moduleInfo.Name,
                ["output"] = outputPath,
                ["outputBytes"] = outInfo.Length.ToString(),
                ["durationMs"] = duration.TotalMilliseconds.ToString("F0", CultureInfo.InvariantCulture),
                ["peakDbFs"] = Db(peak),
                ["headRmsDbFs"] = Db(headRms),
                ["tailRmsDbFs"] = Db(tailRms),
                ["latencySamples"] = plugin.LatencySamples.ToString(),
                ["tailSamples"] = plugin.TailSamples.ToString(),
                ["maxTailSeconds"] = (explicitTail >= 0 ? explicitTail : 30.0).ToString("F2", CultureInfo.InvariantCulture),
            });
    }

    /// <summary>
    /// Parses a <c>"Title=value;Title=value"</c> string and pokes each entry into the plug-in's
    /// parameter collection as a normalised value. Returns the applied list (with the formatted
    /// display string the plug-in would show) so the test output can surface what changed.
    /// </summary>
    private static List<(string Name, double Value, string Display)> ApplyParamOverrides(
        Vst3Plugin plugin, string overrides, out string? error)
    {
        error = null;
        var applied = new List<(string, double, string)>();
        if (string.IsNullOrWhiteSpace(overrides))
        {
            return applied;
        }

        foreach (var raw in overrides.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = raw.IndexOf('=');
            if (eq <= 0 || eq == raw.Length - 1)
            {
                error = $"Malformed --params entry '{raw}'; expected Title=value";
                return applied;
            }
            var name = raw[..eq].Trim();
            var valueStr = raw[(eq + 1)..].Trim();
            if (!double.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                error = $"Cannot parse '{valueStr}' as a normalised value (expected 0..1)";
                return applied;
            }
            var p = plugin.Parameters.FindByTitle(name);
            if (p is null)
            {
                var titles = string.Join(", ", plugin.Parameters.Take(20).Select(x => x.Title));
                error = $"Parameter '{name}' not found. First parameters: {titles}{(plugin.Parameters.Count > 20 ? ", ..." : "")}";
                return applied;
            }
            p.NormalizedValue = value;
            applied.Add((p.Title, value, p.FormatValue(value)));
        }
        return applied;
    }

    private static Vst3ModuleInfo? ResolveModule(string query, out string? error)
    {
        var matches = Vst3PluginScanner.EnumerateInstalled()
            .Where(m => m.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (matches.Count == 0)
        {
            error = $"No installed plug-in matches '{query}'";
            return null;
        }
        if (matches.Count > 1)
        {
            error = $"Multiple matches for '{query}': {string.Join(", ", matches.Take(10).Select(m => m.Name))}";
            return null;
        }
        error = null;
        return matches[0];
    }

    /// <summary>
    /// Maps an arbitrary-channel source onto the plug-in's negotiated input channel count by
    /// up- or down-mixing through NAudio's built-in extension helpers. Mono and stereo are the
    /// only supported targets — wider arrangements are rejected at plug-in init.
    /// </summary>
    private static ISampleProvider AdaptChannels(ISampleProvider source, int targetChannels)
    {
        if (source.WaveFormat.Channels == targetChannels) return source;
        return targetChannels switch
        {
            1 => source.ToMono(),
            2 => source.ToStereo(),
            _ => throw new NotSupportedException($"Cannot adapt to {targetChannels} channels."),
        };
    }

    private static string DescribeTail(uint tail) =>
        tail == 0 ? "none"
        : tail == uint.MaxValue ? "infinite"
        : $"{tail} samples";

    /// <summary>
    /// Reads the rendered WAV back and computes a few sanity signals: total duration, absolute
    /// peak, and RMS over the first / last 200 ms. The head RMS tells us the dry signal was
    /// processed; the tail RMS reveals whether the plug-in is contributing a non-trivial tail.
    /// </summary>
    private static (TimeSpan duration, float peak, float headRms, float tailRms) AnalyseOutput(string path)
    {
        try
        {
            using var reader = new WaveFileReader(path);
            var samples = reader.ToSampleProvider();
            var channels = samples.WaveFormat.Channels;
            var sampleRate = samples.WaveFormat.SampleRate;
            var totalFrames = (int)reader.SampleCount;
            if (totalFrames <= 0)
                return (TimeSpan.Zero, 0, 0, 0);

            var pcm = new float[totalFrames * channels];
            var read = samples.Read(pcm);

            var peak = 0f;
            for (var i = 0; i < read; i++)
            {
                var a = Math.Abs(pcm[i]);
                if (a > peak) peak = a;
            }

            var window = Math.Min(sampleRate / 5, totalFrames); // 200 ms or whole file
            var headRms = Rms(pcm.AsSpan(0, window * channels));
            var tailStart = Math.Max(0, read - (window * channels));
            var tailRms = Rms(pcm.AsSpan(tailStart, read - tailStart));

            return (reader.TotalTime, peak, headRms, tailRms);
        }
        catch
        {
            return (TimeSpan.Zero, 0, 0, 0);
        }
    }

    private static float Rms(Span<float> samples)
    {
        if (samples.Length == 0) return 0;
        double sumSq = 0;
        foreach (var s in samples) sumSq += s * s;
        return (float)Math.Sqrt(sumSq / samples.Length);
    }

    private static string Db(float linear)
        => linear <= 1e-10f
            ? "-inf"
            : (20 * Math.Log10(linear)).ToString("F2", CultureInfo.InvariantCulture);
}
