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
        /// <param name="message"></param>
        /// <param name="timestamp"></param>
        public MidiInMessageEventArgs(int deviceNumber, int message, int timestamp)
        {
            this.DeviceIndex = deviceNumber;
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

        public int DeviceIndex { get; private set; }

        /// <summary>
        /// The Raw message received from the MIDI In API
        /// </summary>
        public int RawMessage { get; private set; }

        /// <summary>
        /// The raw message interpreted as a MidiEvent
        /// </summary>
        public MidiEvent MidiEvent { get; private set; }

        /// <summary>
        /// The timestamp in milliseconds for this message
        /// </summary>
        public int Timestamp { get; private set; }
    }
}