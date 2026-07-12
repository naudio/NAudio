using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi.Interfaces;

/// <summary>
/// IMMNotificationClient. Implementations must be marked with
/// <c>[GeneratedComClass]</c> and declared <c>partial</c> so a CCW vtable
/// can be generated at compile time (NativeAOT / trim safe).
/// </summary>
/// <remarks>
/// These methods are called on a Windows audio system worker thread that holds an
/// internal lock while dispatching the notification. Handlers must return quickly and
/// must <b>not</b> block, wait on another thread, or call back into the audio stack
/// (for example disposing a <c>WaveIn</c>/<c>WaveOut</c>/<c>WasapiOut</c>, or calling
/// back into the device enumerator). Doing so risks a deadlock. To react to a
/// notification, marshal the work to your own thread asynchronously — e.g.
/// <c>Control.BeginInvoke</c> rather than <c>Control.Invoke</c>, or a queued
/// <c>Task.Run</c> — so the callback returns and releases the lock first.
/// </remarks>
[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16),
    Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IMMNotificationClient
{
    /// <summary>
    /// Device State Changed
    /// </summary>
    void OnDeviceStateChanged(string deviceId, DeviceState newState);

    /// <summary>
    /// Device Added
    /// </summary>
    void OnDeviceAdded(string pwstrDeviceId);

    /// <summary>
    /// Device Removed
    /// </summary>
    void OnDeviceRemoved(string deviceId);

    /// <summary>
    /// Default Device Changed
    /// </summary>
    void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId);

    /// <summary>
    /// Property Value Changed
    /// </summary>
    /// <param name="pwstrDeviceId"></param>
    /// <param name="key"></param>
    void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key);
}
