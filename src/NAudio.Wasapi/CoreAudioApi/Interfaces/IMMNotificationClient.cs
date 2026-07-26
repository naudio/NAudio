using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi.Interfaces;

/// <summary>
/// Internal projection of the Core Audio <c>IMMNotificationClient</c> COM callback interface. This is an
/// implementation detail of <see cref="NAudio.CoreAudioApi.MMDeviceNotificationClient"/> — consumers should
/// use that class's events (via <see cref="NAudio.CoreAudioApi.MMDeviceEnumerator.CreateNotificationClient"/>)
/// rather than implementing this interface directly.
/// </summary>
/// <remarks>
/// These methods are called on a Windows audio system worker thread that holds an
/// internal lock while dispatching the notification, so the internal implementation must return quickly
/// and must not block or call back into the audio stack.
/// </remarks>
[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16),
    Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IMMNotificationClient
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
