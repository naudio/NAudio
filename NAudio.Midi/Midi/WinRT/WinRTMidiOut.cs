#if WINRT_MIDI
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Midi;

namespace NAudio.Midi
{
    /// <summary>
    /// Sends MIDI messages using the Windows.Devices.Midi API.
    /// Available on net8.0-windows10.0.19041.0 or later.
    /// </summary>
    public class WinRTMidiOut : IMidiOutput
    {
        private IMidiOutPort port;
        private bool disposed;

        private WinRTMidiOut(IMidiOutPort port)
        {
            this.port = port;
        }

        /// <summary>
        /// Gets the available MIDI output devices
        /// </summary>
        /// <returns>A list of device information</returns>
        public static async Task<IReadOnlyList<DeviceInformation>> GetDevicesAsync()
        {
            var selector = MidiOutPort.GetDeviceSelector();
            return await DeviceInformation.FindAllAsync(selector);
        }

        /// <summary>
        /// Creates a WinRTMidiOut for the specified device
        /// </summary>
        /// <param name="deviceId">The device ID from GetDevicesAsync</param>
        /// <returns>A new WinRTMidiOut instance</returns>
        public static async Task<WinRTMidiOut> CreateAsync(string deviceId)
        {
            var port = await MidiOutPort.FromIdAsync(deviceId);
            if (port == null)
            {
                throw new InvalidOperationException($"Could not open MIDI output device: {deviceId}");
            }
            return new WinRTMidiOut(port);
        }

        /// <summary>
        /// Sends a short MIDI message
        /// </summary>
        /// <param name="message">The packed MIDI message from MidiEvent.GetAsShortMessage()</param>
        public void Send(int message)
        {
            if (disposed) throw new ObjectDisposedException(nameof(WinRTMidiOut));
            var winRtMessage = MidiMessageConverter.ToWinRTMessage(message);
            port.SendMessage(winRtMessage);
        }

        /// <summary>
        /// Sends a long MIDI message (e.g. sysex)
        /// </summary>
        /// <param name="byteBuffer">The bytes to send (including F0/F7 framing for sysex)</param>
        public void SendBuffer(byte[] byteBuffer)
        {
            if (disposed) throw new ObjectDisposedException(nameof(WinRTMidiOut));
            var sysexMessage = MidiMessageConverter.ToWinRTSysexMessage(byteBuffer);
            port.SendMessage(sysexMessage);
        }

        /// <summary>
        /// Dispose the MIDI output port
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                port?.Dispose();
                port = null;
                disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
#endif
