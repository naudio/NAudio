using System.Globalization;
using NAudio.Midi;
using NAudio.Vst3;
using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;

namespace NAudioConsoleTest.Vst3.Tests;

/// <summary>
/// Renders a <b>MIDI file</b> through a chosen VST 3 <b>instrument</b> (VSTi) to a WAV, driving the
/// plug-in through the shared <c>NAudio.Midi</c> playback pipeline — <see cref="MidiFileSequence"/> →
/// <see cref="Vst3MidiInstrument"/> → <see cref="OfflineMidiRenderer"/> — the same path the NAudio
/// sampler uses. The headless validation for Phase 9: a real <c>.mid</c> file plays through a hosted
/// VSTi with sample-accurate event timing, no realtime/audio-hardware involved. The plug-in is resolved
/// by case-insensitive substring match against installed modules and must expose an instrument class.
/// </summary>
sealed class Vst3RenderMidiFileTest : IConsoleTest
{
    public string Id => "Vst3.RenderMidiFile";
    public string Description => "Render a MIDI file through a VST 3 instrument (VSTi) to WAV";
    public MenuPath? MenuLocation => new("VST 3", "Render a MIDI file through a VST 3 instrument", Group: "Instruments", Order: 1);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("plugin", typeof(string), Required: true,
            Help: "instrument name (case-insensitive substring match against installed modules)",
            ChoiceProvider: () => Vst3PluginScanner.EnumerateInstalled().Select(m => m.Name).ToList()),
        new("midi", typeof(string), Required: true, Help: "input MIDI (.mid) path"),
        new("output", typeof(string), Required: true, Help: "output WAV path"),
        new("sampleRate", typeof(int), Required: false, Default: 44100, Help: "render sample rate"),
        new("tailSeconds", typeof(double), Required: false, Default: 2.0, Help: "release-tail margin after the last event (seconds)"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var pluginQuery = ctx.Get<string>("plugin");
        var midiPath = ctx.Get<string>("midi");
        var outputPath = ctx.Get<string>("output");
        var sampleRate = ctx.Get<int>("sampleRate");
        var tailSeconds = ctx.Get<double>("tailSeconds");

        if (!File.Exists(midiPath))
            return TestResult.Fail($"MIDI file not found: {midiPath}");

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

        Console.WriteLine($"  i/o   : {plugin.InputChannelCount}-in / {plugin.OutputChannelCount}-out");

        var sequence = MidiFileSequence.FromFile(midiPath);
        var eventCount = sequence.Timeline.Count;
        Console.WriteLine($"MIDI    : {Path.GetFileName(midiPath)}  ({eventCount} channel events, last tick {sequence.EndTick})");
        if (eventCount == 0)
            return TestResult.Fail("MIDI file has no channel events to play.");

        // The shared pipeline: a VSTi behind IMidiInstrument, driven offline through the sequencer. The
        // tempo map feeds the plug-in a populated ProcessContext (tempo/position) so tempo-following
        // instruments track the file; offline render needs no Transport (monotonic from 0).
        var instrument = new Vst3MidiInstrument(plugin, sequence.TempoMap, sequence.TimeSignatureMap);
        OfflineMidiRenderer.RenderToWaveFile(sequence, instrument, outputPath, tailSeconds);

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
            ["midi"] = midiPath,
            ["midiEvents"] = eventCount.ToString(),
            ["output"] = outputPath,
            ["outputBytes"] = outInfo.Length.ToString(),
            ["peakDbFs"] = Db(peak),
            ["headRmsDbFs"] = Db(headRms),
            ["tailRmsDbFs"] = Db(tailRms),
        };

        if (peak < 1e-4f)
            return TestResult.Fail($"Instrument rendered silence (peak {Db(peak)} dBFS) — the MIDI file produced no audible output", diagnostics);

        return TestResult.Pass($"Rendered {duration} of {instrumentClass.Name} from {Path.GetFileName(midiPath)} (peak {Db(peak)} dBFS)", diagnostics);
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
