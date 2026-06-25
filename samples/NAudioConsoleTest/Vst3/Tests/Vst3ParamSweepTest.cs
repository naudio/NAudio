using System.Globalization;
using NAudio.Vst3;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudioConsoleTest.Shared.Testing;

namespace NAudioConsoleTest.Vst3.Tests;

/// <summary>
/// Phase 3 sanity test: enumerate the plug-in's parameters, then render the same input twice
/// with the chosen parameter set to two different normalised values. The tail RMS of the two
/// renders is compared; a working parameter set produces audibly different output (large dB
/// delta), a no-op or broken set produces near-identical RMS.
/// </summary>
sealed class Vst3ParamSweepTest : IConsoleTest
{
    public string Id => "Vst3.ParamSweep";
    public string Description => "Render a WAV through a VST 3 effect at two parameter values and compare";
    public MenuPath? MenuLocation => new("VST 3", "Compare two parameter values", Group: "Effects", Order: 1);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("plugin", typeof(string), Required: true,
            Help: "plug-in name (case-insensitive substring match against installed modules)",
            ChoiceProvider: () => Vst3PluginScanner.EnumerateInstalled().Select(m => m.Name).ToList()),
        new("input", typeof(string), Required: true, Help: "input WAV path"),
        new("parameter", typeof(string), Required: false, Default: "Mix",
            Help: "name of the parameter to sweep (case-insensitive)"),
        new("low", typeof(double), Required: false, Default: 0.05,
            Help: "low normalised value [0..1]"),
        new("high", typeof(double), Required: false, Default: 0.95,
            Help: "high normalised value [0..1]"),
        new("tailSeconds", typeof(double), Required: false, Default: 2.0,
            Help: "silence to append after source ends so the tail is captured"),
        new("minTailDeltaDb", typeof(double), Required: false, Default: 1.0,
            Help: "minimum tail-RMS difference (dB) between the two renders for PASS"),
        new("dumpParameters", typeof(bool), Required: false, Default: false,
            Help: "if true, prints every parameter (id, title, default, units, flags) before rendering"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var pluginQuery = ctx.Get<string>("plugin");
        var inputPath = ctx.Get<string>("input");
        var paramName = ctx.Get<string>("parameter");
        var low = ctx.Get<double>("low");
        var high = ctx.Get<double>("high");
        var tailSeconds = ctx.Get<double>("tailSeconds");
        var minDeltaDb = ctx.Get<double>("minTailDeltaDb");
        var dumpAll = ctx.Get<bool>("dumpParameters");

        if (!File.Exists(inputPath))
            return TestResult.Fail($"Input not found: {inputPath}");
        if (low < 0 || low > 1 || high < 0 || high > 1)
            return TestResult.Fail("low / high must be in [0, 1]");

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

        using var reader = new WaveFileReader(inputPath);
        var sampleSource = reader.ToSampleProvider();

        const int maxBlockSize = 512;
        using var inspectPlugin = module.CreatePlugin(audioModuleClass, reader.WaveFormat.SampleRate, maxBlockSize);
        Console.WriteLine($"  params: {inspectPlugin.Parameters.Count} ({(inspectPlugin.HasSeparateController ? "separate" : "shared")} controller)");

        if (dumpAll)
        {
            DumpParameters(inspectPlugin);
        }

        var target = inspectPlugin.Parameters.FindByTitle(paramName);
        if (target is null)
        {
            var titles = string.Join(", ", inspectPlugin.Parameters.Take(20).Select(p => p.Title));
            return TestResult.Fail(
                $"Parameter '{paramName}' not found on {moduleInfo.Name}. First {Math.Min(20, inspectPlugin.Parameters.Count)} parameters: {titles}");
        }
        Console.WriteLine($"  sweep : {target.Title} ({target.Units}) — default {target.DefaultNormalizedValue:F3} \"{target.FormatValue(target.DefaultNormalizedValue)}\"");

        // We need fresh plug-in instances per render to make sure internal state (esp. for tail
        // sensitivity) doesn't leak between the two passes.
        var (lowPath, lowRms) = RenderOnce(module, audioModuleClass, reader, sampleSource, target.Id, low, tailSeconds, "low");
        // Reset stream position for the second pass.
        reader.Position = 0;
        sampleSource = reader.ToSampleProvider();
        var (highPath, highRms) = RenderOnce(module, audioModuleClass, reader, sampleSource, target.Id, high, tailSeconds, "high");

        var deltaDb = Math.Abs(DbValue(lowRms) - DbValue(highRms));
        Console.WriteLine($"Tail RMS low ({low:F2}) : {Db(lowRms)} dBFS");
        Console.WriteLine($"Tail RMS high ({high:F2}): {Db(highRms)} dBFS");
        Console.WriteLine($"Δ tail RMS = {deltaDb:F2} dB (threshold {minDeltaDb:F2} dB)");

        var metrics = new Dictionary<string, string>
        {
            ["plugin"] = moduleInfo.Name,
            ["parameter"] = target.Title,
            ["paramId"] = target.Id.ToString(CultureInfo.InvariantCulture),
            ["low"] = low.ToString("F3", CultureInfo.InvariantCulture),
            ["high"] = high.ToString("F3", CultureInfo.InvariantCulture),
            ["lowOutput"] = lowPath,
            ["highOutput"] = highPath,
            ["tailRmsLowDbFs"] = Db(lowRms),
            ["tailRmsHighDbFs"] = Db(highRms),
            ["tailRmsDeltaDb"] = deltaDb.ToString("F2", CultureInfo.InvariantCulture),
        };
        return deltaDb >= minDeltaDb
            ? TestResult.Pass($"{target.Title}: Δ {deltaDb:F2} dB across {low:F2} → {high:F2}", metrics)
            : TestResult.Fail($"Parameter '{target.Title}' only moved tail RMS by {deltaDb:F2} dB (< {minDeltaDb:F2})", metrics);
    }

    private static (string outputPath, float tailRms) RenderOnce(
        Vst3Module module,
        Vst3ClassInfo classInfo,
        WaveFileReader reader,
        ISampleProvider sampleSource,
        uint paramId,
        double normalizedValue,
        double tailSeconds,
        string label)
    {
        using var plugin = module.CreatePlugin(classInfo, reader.WaveFormat.SampleRate, maxBlockSize: 512);
        plugin.Parameters.GetById(paramId).NormalizedValue = normalizedValue;

        var source = AdaptChannels(sampleSource, plugin.InputChannelCount);
        if (tailSeconds > 0)
        {
            source = new OffsetSampleProvider(source) { LeadOut = TimeSpan.FromSeconds(tailSeconds) };
        }

        var outputPath = Path.Combine(Path.GetTempPath(), $"vst3-paramsweep-{label}-{Guid.NewGuid():N}.wav");
        var fx = new Vst3EffectSampleProvider(source, plugin);
        WaveFileWriter.CreateWaveFile(outputPath, fx.ToWaveProvider());

        return (outputPath, MeasureTailRms(outputPath));
    }

    private static float MeasureTailRms(string path)
    {
        using var reader = new WaveFileReader(path);
        var samples = reader.ToSampleProvider();
        var channels = samples.WaveFormat.Channels;
        var totalFrames = (int)reader.SampleCount;
        if (totalFrames <= 0) return 0;

        var pcm = new float[totalFrames * channels];
        var read = samples.Read(pcm);
        var window = Math.Min(samples.WaveFormat.SampleRate / 5, totalFrames); // 200 ms
        var tailStart = Math.Max(0, read - (window * channels));
        return Rms(pcm.AsSpan(tailStart, read - tailStart));
    }

    private static float Rms(Span<float> samples)
    {
        if (samples.Length == 0) return 0;
        double sumSq = 0;
        foreach (var s in samples) sumSq += s * s;
        return (float)Math.Sqrt(sumSq / samples.Length);
    }

    private static double DbValue(float linear)
        => linear <= 1e-10f ? double.NegativeInfinity : 20 * Math.Log10(linear);

    private static string Db(float linear)
    {
        var v = DbValue(linear);
        return double.IsNegativeInfinity(v)
            ? "-inf"
            : v.ToString("F2", CultureInfo.InvariantCulture);
    }

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

    private static void DumpParameters(Vst3Plugin plugin)
    {
        Console.WriteLine("  id          title                          default  units      flags");
        foreach (var p in plugin.Parameters)
        {
            Console.WriteLine(
                $"  0x{p.Id:X8}  {Truncate(p.Title, 30),-30}  {p.DefaultNormalizedValue:F3}    {Truncate(p.Units, 9),-9}  {p.Flags}");
        }
    }

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s[..max];
}
