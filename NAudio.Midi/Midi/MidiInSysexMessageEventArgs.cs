using System;

namespace NAudio.Midi
{
    /// <summary>
    /// MIDI In Sysex Message Information
    /// </summary>
    public class MidiInSysexMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Create a new Sysex MIDI In Message EventArgs
        /// </summary>
        /// <param name="sysexBytes">The Sysex byte array received.</param>
        /// <param name="timestamp">The timestamp at which the message was received.</param>
        public MidiInSysexMessageEventArgs(byte[] sysexBytes, TimeSpan timestamp)
        {
            this.SysexBytes = sysexBytes;
            this.Timestamp = timestamp;
        }

        /// <summary>
        /// The Raw Sysex bytes received in a long MIDI message
        /// </summary>
        public byte[] SysexBytes { get; private set; }

        /// <summary>
        /// The timestamp at which this message was received, measured from
        /// when the input device was opened/started.
        /// </summary>
        public TimeSpan Timestamp { get; private set; }
    }
}
