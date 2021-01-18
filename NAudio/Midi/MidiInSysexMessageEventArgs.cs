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
        /// <param name="message"></param>
        /// <param name="timestamp"></param>
        public MidiInSysexMessageEventArgs(byte[] sysexBytes, int timestamp)
        {
            this.SysexBytes = sysexBytes;
            this.Timestamp = timestamp;
        }

        /// <summary>
        /// The Raw Sysex bytes received in a long MIDI message
        /// </summary>
        public byte[] SysexBytes { get; private set; }


        /// <summary>
        /// The timestamp in milliseconds for this message
        /// </summary>
        public int Timestamp { get; private set; }
    }
}