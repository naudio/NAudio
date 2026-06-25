using System;
using System.Threading.Tasks;
using NAudio.Midi;
using NAudio.Vst3;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace NAudioWpfDemo.Vst3RealtimeInstrumentDemo;

/// <summary>
/// Drives the live-instrument demo: ASIO output (playback-only — no capture) sampling a
/// <see cref="Vst3Plugin"/> instrument, with notes and controllers fed in from a
/// <see cref="WinRTMidiIn"/> device. The instrument is created at the ASIO driver's
/// current sample rate; the chain is <see cref="Vst3InstrumentSampleProvider"/> →
/// <see cref="VolumeSampleProvider"/> (master gain) → ASIO via
/// <see cref="AsioDevice.InitPlayback"/>.
/// </summary>
/// <remarks>
/// The same shape as the CLI <c>Vst3.LiveSynth</c> test, recast as an
/// <c>AsioDevice</c>-backed WPF panel for lower latency than the CLI's
/// shared-mode <c>WasapiPlayer</c> path.
/// </remarks>
sealed class Vst3RealtimeInstrumentEngine : IDisposable
{
    private const int MaxBlockSize = 2048;

    private AsioDevice device;
    private WinRTMidiIn midiIn;
    private Vst3Module module;
    private Vst3Plugin plugin;
    private Vst3PluginView view;
    private VolumeSampleProvider volumeProvider;
    private MeteringSampleProvider meterProvider;
    private int notesPlayed;
    private float outputLevel;

    public bool IsRunning => device != null && device.State == AsioDeviceState.Running;
    public int SampleRate { get; private set; }
    public int FramesPerBuffer { get; private set; }
    public Vst3Plugin Plugin => plugin;
    public Vst3PluginView View => view;
    public int NotesPlayed => notesPlayed;
    /// <summary>Most recent post-master-gain output peak (0..1), refreshed ~10× per second.</summary>
    public float OutputLevel => outputLevel;

    /// <summary>Linear master output gain (1.0 = unity). Safe to set from the UI thread.</summary>
    public float MasterGain
    {
        get => volumeProvider?.Volume ?? 1f;
        set { if (volumeProvider != null) volumeProvider.Volume = value; }
    }

    /// <summary>
    /// Open the ASIO driver in playback-only mode, create the instrument at its negotiated
    /// sample rate, and start the chain. The MIDI device must already have been chosen —
    /// see <see cref="ConnectMidi"/>. Throws on any failure; caller should <see cref="Stop"/>.
    /// </summary>
    public void Start(string asioDriverName, Vst3ModuleInfo moduleInfo, Vst3ClassInfo instrumentClass,
        float initialGain)
    {
        ArgumentNullException.ThrowIfNull(asioDriverName);
        ArgumentNullException.ThrowIfNull(moduleInfo);
        ArgumentNullException.ThrowIfNull(instrumentClass);

        device = AsioDevice.Open(asioDriverName);
        SampleRate = device.CurrentSampleRate;

        module = Vst3Module.Load(moduleInfo.Path);
        plugin = module.CreatePlugin(instrumentClass, SampleRate, MaxBlockSize);
        if (!plugin.IsInstrument)
        {
            throw new InvalidOperationException(
                $"{instrumentClass.Name} did not initialise as an instrument.");
        }

        view = plugin.CreateView();

        var instrumentProvider = new Vst3InstrumentSampleProvider(plugin);
        volumeProvider = new VolumeSampleProvider(instrumentProvider) { Volume = initialGain };
        meterProvider = new MeteringSampleProvider(volumeProvider);
        meterProvider.StreamVolume += OnStreamVolume;

        // ASIO drivers usually want N output channels matching the source — for stereo
        // instruments (the common case) that's channels 0 and 1. AsioPlaybackOptions
        // defaults OutputChannels to Enumerable.Range(0, sourceChannels).
        device.InitPlayback(new AsioPlaybackOptions
        {
            Source = meterProvider.ToWaveProvider(),
        });

        FramesPerBuffer = device.FramesPerBuffer;
        notesPlayed = 0;
        device.Start();
    }

    /// <summary>
    /// Bind a MIDI input device to the running engine. Note on/off/CC/pitch-bend/AT
    /// route into the plug-in via its thread-safe queues; the audio thread sees them on
    /// its next block.
    /// </summary>
    public async Task ConnectMidiAsync(string midiDeviceId)
    {
        DisconnectMidi();
        if (string.IsNullOrEmpty(midiDeviceId)) return;
        var fresh = await WinRTMidiIn.CreateAsync(midiDeviceId).ConfigureAwait(true);
        fresh.MessageReceived += OnMidiMessage;
        midiIn = fresh;
        midiIn.Start();
    }

    public void DisconnectMidi()
    {
        if (midiIn == null) return;
        try { midiIn.Stop(); } catch { /* ignore */ }
        midiIn.MessageReceived -= OnMidiMessage;
        midiIn.Dispose();
        midiIn = null;
    }

    public void Panic()
    {
        plugin?.AllNotesOff();
    }

    public void Stop()
    {
        DisconnectMidi();
        if (device != null)
        {
            try { plugin?.AllNotesOff(); } catch { /* ignore */ }
            try { device.Stop(); } catch { /* may already be stopped */ }
            device.Dispose();
            device = null;
        }
        view?.Dispose();
        view = null;
        plugin?.Dispose();
        plugin = null;
        module?.Dispose();
        module = null;
        if (meterProvider != null) meterProvider.StreamVolume -= OnStreamVolume;
        meterProvider = null;
        volumeProvider = null;
        outputLevel = 0f;
        SampleRate = 0;
        FramesPerBuffer = 0;
    }

    // Runs on the audio thread — keep allocation-free; the float write is atomic on x86/x64.
    private void OnStreamVolume(object sender, StreamVolumeEventArgs e)
    {
        var max = 0f;
        for (var i = 0; i < e.MaxSampleValues.Length; i++)
        {
            var v = e.MaxSampleValues[i];
            if (v > max) max = v;
        }
        outputLevel = max;
    }

    public void Dispose() => Stop();

    private void OnMidiMessage(object sender, MidiInMessageEventArgs e)
    {
        var p = plugin;
        if (p == null) return;
        switch (e.MidiEvent)
        {
            case NoteOnEvent on when on.Velocity > 0:
                System.Threading.Interlocked.Increment(ref notesPlayed);
                p.SendNoteOn(on.NoteNumber, on.Velocity / 127f, on.Channel - 1);
                break;
            case NoteEvent note when note.CommandCode is MidiCommandCode.NoteOff or MidiCommandCode.NoteOn:
                p.SendNoteOff(note.NoteNumber, 0f, note.Channel - 1);
                break;
            case ControlChangeEvent cc:
                p.SendControlChange((int)cc.Controller, cc.ControllerValue / 127.0);
                break;
            case PitchWheelChangeEvent pw:
                p.SendPitchBend(pw.Pitch / 16383.0);
                break;
            case ChannelAfterTouchEvent at:
                p.SendChannelPressure(at.AfterTouchPressure / 127.0);
                break;
            case PatchChangeEvent patch:
                p.SendProgramChange(patch.Patch);
                break;
        }
    }
}
