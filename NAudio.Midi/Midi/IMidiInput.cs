using System;

namespace NAudio.Midi
{
    /// <summary>
    /// Interface for receiving MIDI messages from a device
    /// </summary>
    public interface IMidiInput : IDisposable
    {
        /// <summary>
        /// Called when a MIDI message is received
        /// </summary>
        event EventHandler<MidiInMessageEventArgs> MessageReceived;

        /// <summary>
        /// Called when an invalid MIDI message is received
        /// </summary>
        event EventHandler<MidiInMessageEventArgs> ErrorReceived;

        /// <summary>
        /// Called when a Sysex MIDI message is received
        /// </summary>
        event EventHandler<MidiInSysexMessageEventArgs> SysexMessageReceived;

        /// <summary>
        /// Start receiving MIDI messages
        /// </summary>
        void Start();

        /// <summary>
        /// Stop receiving MIDI messages
        /// </summary>
        void Stop();
    }
}
