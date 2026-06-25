using NAudio.Wave;

namespace NAudio.Midi;

/// <summary>
/// An instrument that consumes MIDI events and renders audio: MIDI in via
/// <see cref="ProcessMidiEvent"/>, audio out via the
/// <see cref="ISampleProvider"/> pull model. Implemented by the NAudio.Sampler
/// engine, and the seam for any other MIDI-driven renderer (a synthesiser, a
/// hosted VST instrument, …) to plug into the same playback hosts:
/// <see cref="SequencedMidiPlayer"/> (timeline playback),
/// <see cref="OfflineMidiRenderer"/> (faster-than-real-time render) and
/// <see cref="LiveMidiInstrument"/> (live input from another thread).
/// </summary>
/// <remarks>
/// Unless an implementation documents otherwise, it is single-threaded:
/// <see cref="ProcessMidiEvent"/> and <c>Read</c> must be called from the same
/// thread, and events take effect on the audio rendered by subsequent
/// <c>Read</c> calls. Hosts that interleave events and audio within a block
/// (as <see cref="SequencedMidiPlayer"/> does) get sample-accurate timing by
/// rendering in segments between events.
/// </remarks>
public interface IMidiInstrument : ISampleProvider
{
    /// <summary>
    /// Dispatches a MIDI event (note on/off, control change, pitch-bend,
    /// aftertouch, program change). Unsupported messages are ignored.
    /// </summary>
    void ProcessMidiEvent(MidiEvent midiEvent);

    /// <summary>
    /// Stops all sound promptly (the MIDI "all sound off" panic), e.g. when a
    /// host seeks or stops. Implementations should avoid clicks where they can
    /// (a short fade) but must not let notes ring out naturally.
    /// </summary>
    void AllSoundOff();
}
