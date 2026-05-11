using System;

namespace NAudio.Midi
{
    /// <summary>
    /// A device-agnostic MIDI output. Implemented by the legacy winmm-backed
    /// <c>MidiOut</c> (in NAudio.WinMM) and the WinRT-backed <c>WinRTMidiOut</c>
    /// (in NAudio.Wasapi).
    /// </summary>
    public interface IMidiOutput : IDisposable
    {
        /// <summary>
        /// Sends a short (1-3 byte) MIDI message packed as a 32-bit integer.
        /// </summary>
        /// <param name="message">The packed MIDI message (status byte, data1, data2).</param>
        void Send(int message);

        /// <summary>
        /// Sends a long MIDI message (e.g. sysex) as a byte buffer.
        /// </summary>
        /// <param name="byteBuffer">The bytes to send, including any framing bytes (e.g. <c>0xF0</c>/<c>0xF7</c> for sysex).</param>
        void SendBuffer(byte[] byteBuffer);
    }
}
