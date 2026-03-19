#if WINRT_MIDI
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Midi;

namespace NAudio.Midi
{
    /// <summary>
    /// Receives MIDI messages using the Windows.Devices.Midi API.
    /// Available on net8.0-windows10.0.19041.0 or later.
    /// </summary>
    public class WinRTMidiIn : IMidiInput
    {
        private MidiInPort port;
        private bool listening;
        private bool disposed;

        /// <summary>
        /// Called when a MIDI message is received
        /// </summary>
        public event EventHandler<MidiInMessageEventArgs> MessageReceived;

        /// <summary>
        /// Called when an invalid MIDI message is received.
        /// Note: the WinRT MIDI API does not raise error events separately.
        /// </summary>
#pragma warning disable CS0067 // Required by IMidiInput interface
        public event EventHandler<MidiInMessageEventArgs> ErrorReceived;
#pragma warning restore CS0067

        /// <summary>
        /// Called when a Sysex MIDI message is received
        /// </summary>
        public event EventHandler<MidiInSysexMessageEventArgs> SysexMessageReceived;

        private WinRTMidiIn(MidiInPort port)
        {
            this.port = port;
        }

        /// <summary>
        /// Gets the available MIDI input devices
        /// </summary>
        /// <returns>A list of device information</returns>
        public static async Task<IReadOnlyList<DeviceInformation>> GetDevicesAsync()
        {
            var selector = MidiInPort.GetDeviceSelector();
            return await DeviceInformation.FindAllAsync(selector);
        }

        /// <summary>
        /// Creates a WinRTMidiIn for the specified device
        /// </summary>
        /// <param name="deviceId">The device ID from GetDevicesAsync</param>
        /// <returns>A new WinRTMidiIn instance</returns>
        public static async Task<WinRTMidiIn> CreateAsync(string deviceId)
        {
            var port = await MidiInPort.FromIdAsync(deviceId);
            if (port == null)
            {
                throw new InvalidOperationException($"Could not open MIDI input device: {deviceId}");
            }
            return new WinRTMidiIn(port);
        }

        /// <summary>
        /// Start receiving MIDI messages
        /// </summary>
        public void Start()
        {
            if (disposed) throw new ObjectDisposedException(nameof(WinRTMidiIn));
            if (!listening)
            {
                port.MessageReceived += Port_MessageReceived;
                listening = true;
            }
        }

        /// <summary>
        /// Stop receiving MIDI messages
        /// </summary>
        public void Stop()
        {
            if (listening)
            {
                port.MessageReceived -= Port_MessageReceived;
                listening = false;
            }
        }

        private void Port_MessageReceived(MidiInPort sender, MidiMessageReceivedEventArgs args)
        {
            var message = args.Message;

            if (message.Type == MidiMessageType.SystemExclusive)
            {
                var sysex = (MidiSystemExclusiveMessage)message;
                byte[] data = sysex.RawData.ToArray();
                SysexMessageReceived?.Invoke(this, new MidiInSysexMessageEventArgs(data, (int)message.Timestamp.TotalMilliseconds));
                return;
            }

            int rawMessage = MidiMessageConverter.ToRawMessage(message);
            var eventArgs = new MidiInMessageEventArgs(rawMessage, (int)message.Timestamp.TotalMilliseconds);
            MessageReceived?.Invoke(this, eventArgs);
        }

        /// <summary>
        /// Dispose the MIDI input port
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                Stop();
                port?.Dispose();
                port = null;
                disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
#endif
