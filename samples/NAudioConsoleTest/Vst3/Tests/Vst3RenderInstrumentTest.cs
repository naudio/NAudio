using System.Globalization;
using NAudio.Vst3;
using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;

namespace NAudioConsoleTest.Vst3.Tests;

/// <summary>
/// Plays a scripted note sequence (an ascending arpeggio) through a chosen VST 3 <b>instrument</b>
/// and writes the result to a WAV — the headless de-risk for Phase 7a (instrument hosting +
/// <c>IEventList</c>), proving that scheduled notes produce audio with no realtime/MIDI/threading
/// involved. The plug-in is resolved by case-insensitive substring match against installed modules
/// and must expose an instrument ("Instrument" sub-category) class.
/// </summary>
internal sealed class Vst3RenderInstrumentTest : IConsoleTest
{
    public string Id => "Vst3.RenderInstrument";
    public string Description => "Render a scripted note sequence through a VST 3 instrument (VSTi)";
    public MenuPath? MenuLocation => new("VST 3", "Render notes through a VST 3 instrument", Group: "Instruments", Order: 0);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("plugin", typeof(string), Required: true,
            Help: "instrument name (case-insensitive substring match against installed modules)",
            ChoiceProvider: () => Vst3PluginScanner.EnumerateInstalled().Select(m => m.Name).ToList()),
        new("output", typeof(string), Required: true, Help: "output WAV path"),
        new("sampleRate", typeof(int), Required: false, Default: 44100, Help: "render sample rate"),
        new("seconds", typeof(double), Required: false, Default: 3.0, Help: "total render length (seconds)"),
        new("velocity", typeof(double), Required: false, Default: 0.8, Help: "note velocity [0,1]"),
    ];

    // C major arpeggio (MIDI note numbers), one note per beat at 120 BPM.
    private static readonly int[] ArpPitches = [60, 64, 67, 72];

    public TestResult Run(TestContext ctx)
    {
        var pluginQuery = ctx.Get<string>("plugin");
        var outputPath = ctx.Get<string>("output");
        var sampleRate = ctx.Get<int>("sampleRate");
        var seconds = ctx.Get<double>("seconds");
        var velocity = (float)ctx.Get<double>("velocity");

        var moduleInfo = ResolveModule(pluginQuery, out var resolveError);
        if (moduleInfo is null)
            return TestResult.Fail(resolveError ?? "Plug-in resolution failed");

        Console.WriteLine($"Plug-in : {moduleInfo.Name}");
        Console.WriteLine($"  path  : {moduleInfo.Path}");

        using var module = Vst3Module.Load(moduleInfo.Path);
        var classes = module.GetClasses();
        var instrumentClass = classes.FirstOrDefault(c => c.IsInstrument);
        if (instrumentClass is null)
        {
            var kinds = string.Join(", ", classes.Where(c => c.IsAudioModule).Select(c => $"{c.Name}={c.Kind}"));
            return TestResult.Fail(
                $"{moduleInfo.Name} exposes no instrument class. Audio modules: {(kinds.Length == 0 ? "(none)" : kinds)}");
        }

        Console.WriteLine($"  class : {instrumentClass.Name}  [{instrumentClass.SubCategories}] -> {instrumentClass.Kind}");

        const int maxBlockSize = 512;
        using var plugin = module.CreatePlugin(instrumentClass, sampleRate, maxBlockSize);
        if (!plugin.IsInstrument)
            return TestResult.Fail($"{instrumentClass.Name} did not initialise as an instrument.");

        var outCh = plugin.OutputChannelCount;
        Console.WriteLine($"  i/o   : {plugin.InputChannelCount}-in / {outCh}-out");
        Console.WriteLine($"  params: {plugin.Parameters.Count} ({(plugin.HasSeparateController ? "separate" : "shared")} controller)");

        // Schedule an ascending arpeggio: one note per beat at 120 BPM, each ringing 90% of a beat.
        const double bpm = 120.0;
        var samplesPerBeat = (long)(sampleRate * 60.0 / bpm);
        var noteLen = (long)(samplesPerBeat * 0.9);
        for (var i = 0; i < ArpPitches.Length; i++)
        {
            var on = i * samplesPerBeat;
            plugin.ScheduleNoteOn(on, ArpPitches[i], velocity);
            plugin.ScheduleNoteOff(on + noteLen, ArpPitches[i]);
        }
        Console.WriteLine($"  notes : {string.Join(", ", ArpPitches)} @ {bpm:F0} BPM, vel {velocity:F2}");

        var totalSamples = (long)(seconds * sampleRate);
        var format = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, outCh);
        var block = new float[maxBlockSize * outCh];
        // Some instruments declare an (ignored) audio input bus; feed it silence.
        var inCh = plugin.InputChannelCount;
        var inBlock = inCh > 0 ? new float[maxBlockSize * inCh] : Array.Empty<float>();
        var produced = 0L;
        using (var writer = new WaveFileWriter(outputPath, format))
        {
            while (produced < totalSamples)
            {
                var n = (int)Math.Min(maxBlockSize, totalSamples - produced);
                var outSpan = block.AsSpan(0, n * outCh);
                outSpan.Clear();
                // inBlock stays zero-filled (VST 3 input buffers are read-only) — feed silence.
                var inSpan = inCh > 0 ? new ReadOnlySpan<float>(inBlock, 0, n * inCh) : ReadOnlySpan<float>.Empty;
                plugin.Process(inSpan, outSpan, n);
                writer.WriteSamples(block, 0, n * outCh);
                produced += n;
            }
        }

        var outInfo = new FileInfo(outputPath);
        var (duration, peak, headRms, tailRms) = AnalyseOutput(outputPath);
        Console.WriteLine($"Output  : {outputPath}");
        Console.WriteLine($"  size  : {outInfo.Length:N0} bytes");
        Console.WriteLine($"  length: {duration}");
        Console.WriteLine($"  peak  : {Db(peak)} dBFS");
        Console.WriteLine($"  head 200ms RMS: {Db(headRms)} dBFS");
        Console.WriteLine($"  tail 200ms RMS: {Db(tailRms)} dBFS");

        var diagnostics = new Dictionary<string, string>
        {
            ["plugin"] = moduleInfo.Name,
            ["class"] = instrumentClass.Name,
            ["output"] = outputPath,
            ["outputBytes"] = outInfo.Length.ToString(),
            ["peakDbFs"] = Db(peak),
            ["headRmsDbFs"] = Db(headRms),
            ["tailRmsDbFs"] = Db(tailRms),
        };

        // The whole point of the de-risk is "notes produce audio" — silence is a failure.
        if (peak < 1e-4f)
            return TestResult.Fail($"Instrument rendered silence (peak {Db(peak)} dBFS) — notes produced no audible output", diagnostics);

        return TestResult.Pass($"Rendered {duration} of {instrumentClass.Name} (peak {Db(peak)} dBFS)", diagnostics);
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

            var window = Math.Min(sampleRate / 5, totalFrames);
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
