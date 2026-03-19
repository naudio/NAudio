using System;

namespace NAudio.Midi
{
    /// <summary>
    /// Interface for sending MIDI messages to a device
    /// </summary>
    public interface IMidiOutput : IDisposable
    {
        /// <summary>
        /// Sends a short MIDI message (packed as a 32-bit integer)
        /// </summary>
        /// <param name="message">The packed MIDI message</param>
        void Send(int message);

        /// <summary>
        /// Sends a long MIDI message (e.g. sysex)
        /// </summary>
        /// <param name="byteBuffer">The bytes to send</param>
        void SendBuffer(byte[] byteBuffer);
    }
}
