using System;

namespace NAudio.Midi;

/// <summary>
/// Convenience extensions on <see cref="IMidiOutput"/>.
/// </summary>
public static class MidiOutputExtensions
{
    /// <summary>
    /// Sends a <see cref="MidiEvent"/> as a short MIDI message.
    /// </summary>
    /// <remarks>
    /// Only valid for events that fit in a 1-3 byte short message (channel messages and system real-time messages).
    /// Sysex events should be sent via <see cref="IMidiOutput.SendBuffer"/>.
    /// </remarks>
    public static void Send(this IMidiOutput output, MidiEvent midiEvent)
    {
        if (output == null) throw new ArgumentNullException(nameof(output));
        if (midiEvent == null) throw new ArgumentNullException(nameof(midiEvent));
        output.Send(midiEvent.GetAsShortMessage());
    }
}
