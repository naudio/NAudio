using NAudio.Midi;
using NAudio.Vst3;
using NAudio.Wave;
using NAudioConsoleTest.Shared.Testing;
using NAudioConsoleTest.Wasapi.Tests;
using Spectre.Console;
using Windows.Devices.Enumeration;

namespace NAudioConsoleTest.Vst3.Tests;

/// <summary>
/// Plays a hosted VST 3 instrument (VSTi) live from a MIDI keyboard — the Phase 7b headline.
/// Captures MIDI via the WinRT-backed <see cref="WinRTMidiIn"/>, routes note on/off into the
/// plug-in with <see cref="Vst3Plugin.SendNoteOn"/> / <c>SendNoteOff</c>, and renders the synth's
/// output to the speakers through <c>WasapiPlayer</c> (shared mode).
/// </summary>
sealed class Vst3LiveSynthTest : IConsoleTest
{
    public string Id => "Vst3.LiveSynth";
    public string Description => "Play a VST 3 instrument live from a MIDI keyboard";
    public MenuPath? MenuLocation => new("VST 3", "Play a VST 3 instrument live (MIDI)", Group: "Instruments", Order: 1);

    public IReadOnlyList<TestParameter> Parameters =>
    [
        new("plugin", typeof(string), Required: true,
            Help: "instrument name (case-insensitive substring match)",
            ChoiceProvider: () => Vst3PluginScanner.EnumerateInstalled().Select(m => m.Name).ToList()),
        new("midiDevice", typeof(string), Required: false, Default: "",
            Help: "MIDI input name substring (default: first available)"),
        new("renderDevice", typeof(string), Required: false, Default: WasapiDevices.DefaultMarker,
            Help: "render endpoint friendly name (or 'default')",
            ChoiceProvider: WasapiDevices.RenderDeviceNames),
        new("sampleRate", typeof(int), Required: false, Default: 44100, Help: "instrument render sample rate"),
        new("duration", typeof(TimeSpan), Required: false, Default: TimeSpan.FromMinutes(5),
            Help: "max session length (ESC stops early)"),
    ];

    public TestResult Run(TestContext ctx)
    {
        var pluginQuery = ctx.Get<string>("plugin");
        var midiQuery = ctx.Get<string>("midiDevice");
        var renderDeviceName = ctx.Get<string>("renderDevice");
        var sampleRate = ctx.Get<int>("sampleRate");
        var duration = ctx.Get<TimeSpan>("duration");

        // --- Resolve the instrument plug-in ---
        var matches = Vst3PluginScanner.EnumerateInstalled()
            .Where(m => m.Name.Contains(pluginQuery, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (matches.Count == 0) return TestResult.Fail($"No installed plug-in matches '{pluginQuery}'");
        if (matches.Count > 1) return TestResult.Fail($"Multiple matches: {string.Join(", ", matches.Take(10).Select(m => m.Name))}");
        var moduleInfo = matches[0];

        using var module = Vst3Module.Load(moduleInfo.Path);
        var instrumentClass = module.GetClasses().FirstOrDefault(c => c.IsInstrument);
        if (instrumentClass is null)
            return TestResult.Fail($"{moduleInfo.Name} exposes no instrument class.");

        // --- Resolve the render device ---
        var renderDevice = WasapiDevices.ResolveRender(renderDeviceName);
        if (renderDevice is null) return TestResult.Fail($"Render device not found: {renderDeviceName}");

        // --- Resolve the MIDI input device (WinRT enumeration is async) ---
        var midiDevices = WinRTMidiIn.GetDevicesAsync().GetAwaiter().GetResult();
        if (midiDevices.Count == 0)
            return TestResult.Skipped("No MIDI input devices found — connect a keyboard and retry.");

        Console.WriteLine("MIDI inputs:");
        foreach (var d in midiDevices) Console.WriteLine($"    - {d.Name}");
        var chosenMidi = ResolveMidiDevice(midiDevices, midiQuery, out var midiError);
        if (chosenMidi is null)
            return TestResult.Fail(midiError!);

        WasapiVolumeSafety.CapAt(renderDevice);

        const int maxBlockSize = 512;
        using var plugin = module.CreatePlugin(instrumentClass, sampleRate, maxBlockSize);
        if (!plugin.IsInstrument)
            return TestResult.Fail($"{instrumentClass.Name} did not initialise as an instrument.");

        Console.WriteLine($"Instrument : {instrumentClass.Name}  [{instrumentClass.SubCategories}]");
        Console.WriteLine($"  i/o      : {plugin.InputChannelCount}-in / {plugin.OutputChannelCount}-out @ {sampleRate} Hz");
        Console.WriteLine($"  controls : {(plugin.SupportsMidiControllers ? "pitch-bend / mod-wheel / CC → parameters" : "none (plug-in has no IMidiMapping)")}");
        Console.WriteLine($"MIDI in    : {chosenMidi.Name}");
        Console.WriteLine($"Render     : {renderDevice.FriendlyName}");

        var noteOnCount = 0;
        using var midiIn = WinRTMidiIn.CreateAsync(chosenMidi.Id).GetAwaiter().GetResult();
        midiIn.MessageReceived += (_, e) =>
        {
            switch (e.MidiEvent)
            {
                // Note-on with non-zero velocity → play.
                case NoteOnEvent on when on.Velocity > 0:
                    Interlocked.Increment(ref noteOnCount);
                    plugin.SendNoteOn(on.NoteNumber, on.Velocity / 127f, on.Channel - 1);
                    break;
                // Note-off, or note-on with velocity 0 (running-status note-off) → release.
                case NoteEvent note when note.CommandCode is MidiCommandCode.NoteOff or MidiCommandCode.NoteOn:
                    plugin.SendNoteOff(note.NoteNumber, 0f, note.Channel - 1);
                    break;
                // Expression: routed to parameters via the plug-in's IMidiMapping.
                case ControlChangeEvent cc:
                    plugin.SendControlChange((int)cc.Controller, cc.ControllerValue / 127.0);
                    break;
                case PitchWheelChangeEvent pw:
                    plugin.SendPitchBend(pw.Pitch / 16383.0); // 0..16383, 8192 = centre
                    break;
                case ChannelAfterTouchEvent at:
                    plugin.SendChannelPressure(at.AfterTouchPressure / 127.0);
                    break;
            }
        };

        var instrumentSource = new Vst3InstrumentSampleProvider(plugin);

        using var player = new WasapiPlayerBuilder()
            .WithDevice(renderDevice)
            .WithSharedMode()
            .WithEventSync()
            .WithMmcssThreadPriority("Pro Audio")
            .Build();
        player.Init(instrumentSource.ToWaveProvider());

        player.Play();
        midiIn.Start();

        AnsiConsole.MarkupLine(
            $"\n[bold green]Live[/] — play your keyboard. "
            + $"[dim]({(ctx.Interactive ? "ESC stops, Space = all-notes-off panic" : "Ctrl+C stops")}; auto-stop in {duration.TotalSeconds:F0}s)[/]");

        var start = DateTime.UtcNow;
        while (player.PlaybackState != PlaybackState.Stopped)
        {
            if (ctx.Interactive && Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true).Key;
                if (key == ConsoleKey.Escape) break;
                if (key == ConsoleKey.Spacebar)
                {
                    plugin.AllNotesOff();
                    AnsiConsole.MarkupLine("[yellow]panic — all notes off[/]");
                }
            }
            if (ctx.Cancellation.WaitHandle.WaitOne(100)) break;
            if (DateTime.UtcNow - start >= duration) break;
        }

        // Release any held notes, give the audio thread a moment to flush them, then stop.
        midiIn.Stop();
        plugin.AllNotesOff();
        Thread.Sleep(50);
        player.Stop();
        var elapsed = DateTime.UtcNow - start;

        var diagnostics = new Dictionary<string, string>
        {
            ["plugin"] = moduleInfo.Name,
            ["midiDevice"] = chosenMidi.Name,
            ["renderDevice"] = renderDevice.FriendlyName,
            ["notesPlayed"] = noteOnCount.ToString(),
            ["elapsedSeconds"] = elapsed.TotalSeconds.ToString("F1"),
        };

        return ctx.Cancellation.IsCancellationRequested
            ? TestResult.Skipped($"Cancelled after {elapsed.TotalSeconds:F1}s ({noteOnCount} notes)")
            : TestResult.Pass($"Played {instrumentClass.Name} for {elapsed.TotalSeconds:F0}s ({noteOnCount} notes)", diagnostics);
    }

    /// <summary>
    /// Resolves a MIDI input by name. Empty query → first device. Otherwise an exact
    /// (case-insensitive) name match wins — important because one keyboard often exposes several
    /// ports whose names all contain the device name (e.g. "MODX M" and "MIDIIN2 (MODX M)"). Falls
    /// back to a substring match only when it is unambiguous.
    /// </summary>
    private static DeviceInformation? ResolveMidiDevice(
        IReadOnlyList<DeviceInformation> devices, string query, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(query))
            return devices[0];

        var exact = devices.FirstOrDefault(d => string.Equals(d.Name, query, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
            return exact;

        var matches = devices.Where(d => d.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count == 1)
            return matches[0];
        if (matches.Count == 0)
            error = $"No MIDI input matches '{query}'. Available: {string.Join(", ", devices.Select(d => d.Name))}";
        else
            error = $"'{query}' matches several MIDI inputs: {string.Join(", ", matches.Select(d => d.Name))}. "
                + "Use the exact device name.";
        return null;
    }
}
