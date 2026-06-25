using System;
using NAudio.Midi;
using NAudio.Sequencing;
using NAudio.Wave;

namespace NAudio.Vst3;

/// <summary>
/// Adapts a hosted VST 3® <b>instrument</b> (VSTi) to NAudio's <see cref="IMidiInstrument"/> seam, so it
/// plugs into the shared MIDI playback hosts in <c>NAudio.Midi</c> — <see cref="SequencedMidiPlayer"/>
/// (timeline / <c>.mid</c>-file playback), <see cref="OfflineMidiRenderer"/> (faster-than-real-time
/// render to WAV) and <see cref="LiveMidiInstrument"/> (live input from another thread) — exactly as the
/// <c>NAudio.Sampler</c> engine does. Feed it a <see cref="MidiFileSequence"/> through one of those hosts
/// and a VSTi renders or plays the file with sample-accurate event timing.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IMidiInstrument"/> is a <em>segment-render</em> contract: <see cref="ProcessMidiEvent"/>
/// applies an event, then the next <see cref="Read"/> renders forward with it in effect. A host such as
/// <see cref="SequencedMidiPlayer"/> splits each audio block at event boundaries, so every dispatched
/// event belongs at the start of the following (sub-)block. This adapter therefore translates each MIDI
/// event onto <see cref="Vst3Plugin"/>'s offset-0 immediate-event path (<see cref="Vst3Plugin.EnqueueNoteOn"/>
/// et al.) rather than the wall-clock <c>Send*</c> path, which keeps timing correct under offline
/// rendering where there is no real-time clock.
/// </para>
/// <para>
/// Single-threaded, like the rest of <see cref="IMidiInstrument"/>: call <see cref="ProcessMidiEvent"/>
/// and <see cref="Read"/> from the same thread. Calling <see cref="ProcessMidiEvent"/> directly from
/// another thread is not merely unsynchronised — an event racing <see cref="AllSoundOff"/> can leave a
/// note stuck on. To drive a VSTi from live MIDI input arriving on another thread, wrap this in a
/// <see cref="LiveMidiInstrument"/>, which marshals events onto the render thread for you.
/// </para>
/// <para>
/// Instruments take no audio input, so the plug-in's input bus (if present) receives silence. Vocoders
/// and other audio-consuming synths aren't covered here — use <see cref="Vst3InstrumentSampleProvider"/>
/// for that.
/// </para>
/// <para>
/// Pass a tempo map (and optionally a <see cref="TimeSignatureMap"/> and the driving
/// <see cref="Transport"/>) to feed the plug-in a populated <c>ProcessContext</c> — tempo, time
/// signature, musical position and playing state — so tempo-following plug-ins (arpeggiators, synced
/// delays) lock to the timeline. With a <see cref="Transport"/> the playhead and playing state come from
/// it (so seek / stop / loop are handled for free); without one the adapter counts rendered frames
/// itself (the right model for a monotonic offline render via <see cref="OfflineMidiRenderer"/>). Omit
/// the tempo map entirely (the single-argument constructor) and the plug-in free-runs — the right choice
/// for live keyboard input with no timeline.
/// </para>
/// <para>
/// When you supply a <see cref="Transport"/>, this adapter only <em>reads</em> its position; it does not
/// move it. Whatever drives playback must advance the transport by the number of frames each
/// <see cref="Read"/> consumes (e.g. a host such as <see cref="SequencedMidiPlayer"/> does this), or the
/// playhead — and the <c>ProcessContext</c> position it feeds the plug-in — never progresses.
/// </para>
/// </remarks>
public sealed class Vst3MidiInstrument : IMidiInstrument
{
    private readonly Vst3Plugin _plugin;
    private readonly int _outputChannels;
    private readonly int _inputChannels;
    private readonly int _maxBlockSize;
    private readonly float[] _inputBlock;
    private readonly float[] _outputBlock;
    private readonly ITempoMap? _tempoMap;
    private readonly TimeSignatureMap? _timeSignatureMap;
    private readonly Transport? _transport;
    private long _selfFrames; // playhead when no Transport drives position (offline render)

    /// <summary>Wraps an instrument plug-in as an <see cref="IMidiInstrument"/> with no musical context
    /// (the plug-in free-runs) — the right choice for live keyboard input.</summary>
    /// <param name="plugin">An instrument plug-in (see <see cref="Vst3Plugin.IsInstrument"/>).</param>
    /// <exception cref="ArgumentException">The plug-in is not an instrument.</exception>
    public Vst3MidiInstrument(Vst3Plugin plugin) : this(plugin, null, null, null)
    {
    }

    /// <summary>
    /// Wraps an instrument plug-in as an <see cref="IMidiInstrument"/> that feeds it a populated
    /// <c>ProcessContext</c> built from the sequencer timing types.
    /// </summary>
    /// <param name="plugin">An instrument plug-in (see <see cref="Vst3Plugin.IsInstrument"/>).</param>
    /// <param name="tempoMap">Drives tempo and musical position. When <c>null</c> the plug-in free-runs.</param>
    /// <param name="timeSignatureMap">Optional; drives the time signature and bar position. Defaults to 4/4.</param>
    /// <param name="transport">
    /// Optional; when supplied, the playhead and playing state are read from it each block (so seek / stop /
    /// loop are honoured). When <c>null</c> the adapter counts rendered frames itself and is always playing —
    /// the correct model for a monotonic offline render.
    /// </param>
    /// <exception cref="ArgumentException">The plug-in is not an instrument.</exception>
    public Vst3MidiInstrument(Vst3Plugin plugin, ITempoMap? tempoMap,
        TimeSignatureMap? timeSignatureMap = null, Transport? transport = null)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        if (!plugin.IsInstrument)
        {
            throw new ArgumentException("Plug-in is not an instrument (no event input bus).", nameof(plugin));
        }

        _plugin = plugin;
        _outputChannels = plugin.OutputChannelCount;
        _inputChannels = plugin.InputChannelCount;
        _maxBlockSize = plugin.MaxBlockSize;
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(plugin.SampleRate, _outputChannels);
        _outputBlock = new float[_maxBlockSize * _outputChannels];
        _inputBlock = new float[_maxBlockSize * Math.Max(1, _inputChannels)];
        _tempoMap = tempoMap ?? transport?.TempoMap;
        _timeSignatureMap = timeSignatureMap;
        _transport = transport;
    }

    /// <summary>The wrapped plug-in.</summary>
    public Vst3Plugin Plugin => _plugin;

    /// <inheritdoc/>
    public WaveFormat WaveFormat { get; }

    /// <inheritdoc/>
    public void ProcessMidiEvent(MidiEvent midiEvent)
    {
        switch (midiEvent)
        {
            // NoteOnEvent derives from NoteEvent, so it must be matched first. A note-on with
            // velocity 0 is the running-status note-off convention — fall through to the off case.
            case NoteOnEvent noteOn when noteOn.Velocity > 0:
                _plugin.EnqueueNoteOn(noteOn.NoteNumber, noteOn.Velocity / 127f, noteOn.Channel - 1);
                break;
            case NoteEvent note when note.CommandCode is MidiCommandCode.NoteOn or MidiCommandCode.NoteOff:
                _plugin.EnqueueNoteOff(note.NoteNumber, 0f, note.Channel - 1);
                break;
            case ControlChangeEvent cc:
                _plugin.EnqueueControlChange((int)cc.Controller, cc.ControllerValue / 127.0);
                break;
            case PitchWheelChangeEvent pitch:
                // NAudio pitch is 0..16383 (8192 = centre); the plug-in wants 0..1 (0.5 = centre).
                _plugin.EnqueuePitchBend(pitch.Pitch / 16383.0);
                break;
            case ChannelAfterTouchEvent aftertouch:
                _plugin.EnqueueChannelPressure(aftertouch.AfterTouchPressure / 127.0);
                break;
            case PatchChangeEvent patch:
                _plugin.EnqueueProgramChange(patch.Patch);
                break;
            case SysexEvent sysex:
                _plugin.EnqueueSysEx(FrameSysEx(sysex));
                break;
                // Any other message (remaining meta events) is ignored, per the IMidiInstrument contract.
        }
    }

    // NAudio stores a sysex payload without its 0xF0/0xF7 framing; VST 3's DataEvent wants the complete
    // MIDI message, so re-wrap it.
    private static byte[] FrameSysEx(SysexEvent sysex)
    {
        var payload = sysex.Data;
        var framed = new byte[payload.Length + 2];
        framed[0] = 0xF0;
        payload.CopyTo(framed, 1);
        framed[^1] = 0xF7;
        return framed;
    }

    /// <inheritdoc/>
    public void AllSoundOff() => _plugin.AllNotesOff();

    /// <inheritdoc/>
    /// <remarks>
    /// <paramref name="buffer"/>'s length must be a whole number of frames (a multiple of
    /// <see cref="WaveFormat"/>'s channel count). A non-frame-aligned length silently drops the
    /// trailing partial frame, which — because an instrument is a never-ending source — can make a
    /// caller that treats a short read as end-of-stream stop early.
    /// </remarks>
    public int Read(Span<float> buffer)
    {
        var count = buffer.Length;
        var produced = 0;
        var pushContext = _tempoMap is not null;
        while (produced < count)
        {
            var frames = Math.Min(_maxBlockSize, (count - produced) / _outputChannels);
            if (frames == 0)
            {
                break; // remaining < one frame (count is normally channel-aligned)
            }

            if (pushContext)
            {
                // Position + playing state: from the Transport (authoritative — seek/stop/loop) or, when
                // none drives us, a self-counter that advances per rendered chunk (monotonic offline).
                var frame = _transport?.CurrentFrames ?? _selfFrames;
                var playing = _transport?.IsPlaying ?? true;
                _plugin.SetMusicalContext(BuildContext(frame, playing));
                if (_transport is null && playing) _selfFrames += frames;
            }

            ReadOnlySpan<float> inputSpan;
            if (_inputChannels > 0)
            {
                var inputCount = frames * _inputChannels;
                Array.Clear(_inputBlock, 0, inputCount); // instruments get silence on their input bus
                inputSpan = _inputBlock.AsSpan(0, inputCount);
            }
            else
            {
                inputSpan = ReadOnlySpan<float>.Empty;
            }

            var outputCount = frames * _outputChannels;
            var outputSpan = _outputBlock.AsSpan(0, outputCount);
            outputSpan.Clear();
            _plugin.Process(inputSpan, outputSpan, frames);
            outputSpan.CopyTo(buffer.Slice(produced, outputCount));
            produced += outputCount;
        }
        return produced;
    }

    // Maps a playhead (in frames) + playing state to a ProcessContext snapshot via the tempo / time-
    // signature maps. The tempo map gives the BPM and musical (quarter-note) position; the time-signature
    // map (or a 4/4 default) gives the meter and the current bar's start position.
    private Vst3MusicalContext BuildContext(long frame, bool playing)
    {
        var tempoMap = _tempoMap!; // only called when pushContext (i.e. _tempoMap is non-null)
        var seconds = frame / (double)WaveFormat.SampleRate;
        var tick = tempoMap.TicksFromSeconds(seconds);
        var tempo = tempoMap.BpmAtTicks(tick);
        var projectTimeMusic = tick / (double)MusicalTime.CanonicalPpq;

        int numerator, denominator;
        long barStartTick;
        if (_timeSignatureMap is not null)
        {
            var signature = _timeSignatureMap.SignatureAt(tick);
            numerator = signature.Numerator;
            denominator = signature.Denominator;
            var position = _timeSignatureMap.FromTicks(tick);
            barStartTick = _timeSignatureMap.ToTicks(new BarBeatTick(position.Bar, 1, 0));
        }
        else
        {
            numerator = 4;
            denominator = 4;
            var ticksPerBar = 4L * MusicalTime.CanonicalPpq; // 4/4
            barStartTick = tick / ticksPerBar * ticksPerBar;
        }

        return new Vst3MusicalContext(
            playing, frame, projectTimeMusic,
            barStartTick / (double)MusicalTime.CanonicalPpq, tempo, numerator, denominator);
    }
}
