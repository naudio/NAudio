using System;

namespace NAudio.Midi;

/// <summary>
/// A device-agnostic MIDI input. Implemented by the legacy winmm-backed
/// <c>MidiIn</c> (in NAudio.WinMM) and the WinRT-backed <c>WinRTMidiIn</c>
/// (in NAudio.Wasapi).
/// </summary>
public interface IMidiInput : IDisposable
{
    /// <summary>
    /// Raised when a short MIDI message is received.
    /// </summary>
    event EventHandler<MidiInMessageEventArgs> MessageReceived;

    /// <summary>
    /// Raised when a sysex MIDI message is received.
    /// </summary>
    event EventHandler<MidiInSysexMessageEventArgs> SysexMessageReceived;

    /// <summary>
    /// Start receiving MIDI messages.
    /// </summary>
    void Start();

    /// <summary>
    /// Stop receiving MIDI messages.
    /// </summary>
    void Stop();
}
