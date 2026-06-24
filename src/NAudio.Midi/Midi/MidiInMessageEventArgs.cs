using System;

namespace NAudio.Midi
{
    /// <summary>
    /// MIDI In Message Information
    /// </summary>
    public class MidiInMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Create a new MIDI In Message EventArgs
        /// </summary>
        /// <param name="message">The raw short MIDI message (status, data1, data2 packed into a 32-bit int).</param>
        /// <param name="timestamp">The timestamp at which the message was received.</param>
        public MidiInMessageEventArgs(int message, TimeSpan timestamp)
        {
            this.RawMessage = message;
            this.Timestamp = timestamp;
            try
            {
                this.MidiEvent = MidiEvent.FromRawMessage(message);
            }
            catch (Exception)
            {
                // don't worry too much - might be an invalid message
            }
        }

        /// <summary>
        /// The Raw message received from the MIDI In API
        /// </summary>
        public int RawMessage { get; private set; }

        /// <summary>
        /// The raw message interpreted as a MidiEvent
        /// </summary>
        public MidiEvent MidiEvent { get; private set; }

        /// <summary>
        /// The timestamp at which this message was received, measured from
        /// when the input device was opened/started.
        /// </summary>
        public TimeSpan Timestamp { get; private set; }
    }
}
