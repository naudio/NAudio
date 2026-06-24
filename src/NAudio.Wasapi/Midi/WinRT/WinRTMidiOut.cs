using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Midi;

namespace NAudio.Midi
{
    /// <summary>
    /// MIDI output backed by the WinRT <c>Windows.Devices.Midi</c> API.
    /// </summary>
    /// <remarks>
    /// Construction is async — use <see cref="GetDevicesAsync"/> to enumerate devices and
    /// <see cref="CreateAsync"/> to open one.
    /// </remarks>
    public class WinRTMidiOut : IMidiOutput
    {
        private IMidiOutPort port;
        private bool disposed;

        private WinRTMidiOut(IMidiOutPort port)
        {
            this.port = port;
        }

        /// <summary>
        /// Enumerates the available MIDI output devices.
        /// </summary>
        public static async Task<IReadOnlyList<DeviceInformation>> GetDevicesAsync()
        {
            var selector = MidiOutPort.GetDeviceSelector();
            return await DeviceInformation.FindAllAsync(selector);
        }

        /// <summary>
        /// Opens the MIDI output device with the given identifier.
        /// </summary>
        /// <param name="deviceId">A device identifier from <see cref="GetDevicesAsync"/>.</param>
        public static async Task<WinRTMidiOut> CreateAsync(string deviceId)
        {
            var port = await MidiOutPort.FromIdAsync(deviceId);
            if (port == null)
            {
                throw new InvalidOperationException($"Could not open MIDI output device: {deviceId}");
            }
            return new WinRTMidiOut(port);
        }

        /// <inheritdoc />
        public void Send(int message)
        {
            if (disposed) throw new ObjectDisposedException(nameof(WinRTMidiOut));
            port.SendMessage(MidiMessageConverter.ToWinRTMessage(message));
        }

        /// <inheritdoc />
        public void SendBuffer(byte[] byteBuffer)
        {
            if (disposed) throw new ObjectDisposedException(nameof(WinRTMidiOut));
            port.SendMessage(MidiMessageConverter.ToWinRTSysexMessage(byteBuffer));
        }

        /// <inheritdoc />
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
