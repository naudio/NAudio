using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.Wasapi.CoreAudioApi;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// IMMNotificationClient. Implementations must be marked with
    /// <c>[GeneratedComClass]</c> and declared <c>partial</c> so a CCW vtable
    /// can be generated at compile time (NativeAOT / trim safe).
    /// </summary>
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

}
