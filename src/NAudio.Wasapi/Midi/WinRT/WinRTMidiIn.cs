using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Midi;

namespace NAudio.Midi;

/// <summary>
/// MIDI input backed by the WinRT <c>Windows.Devices.Midi</c> API.
/// </summary>
/// <remarks>
/// Construction is async — use <see cref="GetDevicesAsync"/> to enumerate devices and
/// <see cref="CreateAsync"/> to open one. Timestamps reach the event handler as <see cref="TimeSpan"/>
/// values relative to when the underlying port was opened, with full WinRT 100 ns resolution preserved.
/// </remarks>
public class WinRTMidiIn : IMidiInput
{
    private MidiInPort port;
    private bool listening;
    private bool disposed;

    /// <inheritdoc />
    public event EventHandler<MidiInMessageEventArgs> MessageReceived;

    /// <inheritdoc />
    public event EventHandler<MidiInSysexMessageEventArgs> SysexMessageReceived;

    private WinRTMidiIn(MidiInPort port)
    {
        this.port = port;
    }

    /// <summary>
    /// Enumerates the available MIDI input devices.
    /// </summary>
    public static async Task<IReadOnlyList<DeviceInformation>> GetDevicesAsync()
    {
        var selector = MidiInPort.GetDeviceSelector();
        return await DeviceInformation.FindAllAsync(selector);
    }

    /// <summary>
    /// Opens the MIDI input device with the given identifier.
    /// </summary>
    /// <param name="deviceId">A device identifier from <see cref="GetDevicesAsync"/>.</param>
    public static async Task<WinRTMidiIn> CreateAsync(string deviceId)
    {
        var port = await MidiInPort.FromIdAsync(deviceId);
        if (port == null)
        {
            throw new InvalidOperationException($"Could not open MIDI input device: {deviceId}");
        }
        return new WinRTMidiIn(port);
    }

    /// <inheritdoc />
    public void Start()
    {
        if (disposed) throw new ObjectDisposedException(nameof(WinRTMidiIn));
        if (!listening)
        {
            port.MessageReceived += Port_MessageReceived;
            listening = true;
        }
    }

    /// <inheritdoc />
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
            SysexMessageReceived?.Invoke(this, new MidiInSysexMessageEventArgs(data, message.Timestamp));
            return;
        }

        int rawMessage = MidiMessageConverter.ToRawMessage(message);
        MessageReceived?.Invoke(this, new MidiInMessageEventArgs(rawMessage, message.Timestamp));
    }

    /// <inheritdoc />
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
