using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Entry point to the Windows Core Audio device enumeration API
    /// (<c>IMMDeviceEnumerator</c>). Use this to discover audio endpoints,
    /// resolve the default render or capture device, look up a device by ID,
    /// and subscribe to endpoint change notifications.
    /// </summary>
    public class MMDeviceEnumerator : IDisposable
    {
        // CLSID_MMDeviceEnumerator — mmdeviceapi.h
        private static readonly Guid CLSID_MMDeviceEnumerator = new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E");
        // IID_IMMDeviceEnumerator — mmdeviceapi.h
        private static readonly Guid IID_IMMDeviceEnumerator = new Guid("A95664D2-9614-4F35-A746-DE8DB63617E6");

        private IMMDeviceEnumerator realEnumerator;

        /// <summary>
        /// Activates the system <c>MMDeviceEnumerator</c> COM object.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// Thrown on Windows versions earlier than Vista, where Core Audio is not available.
        /// </exception>
        public MMDeviceEnumerator()
        {
            if (Environment.OSVersion.Version.Major < 6)
            {
                throw new NotSupportedException("Core Audio device enumeration requires Windows Vista or newer.");
            }
            realEnumerator = ComActivation.CreateInstance<IMMDeviceEnumerator>(
                CLSID_MMDeviceEnumerator, IID_IMMDeviceEnumerator);
        }

        /// <summary>
        /// Returns every audio endpoint that matches the supplied direction and state mask.
        /// </summary>
        /// <param name="dataFlow">Render, capture, or both.</param>
        /// <param name="dwStateMask">Bitmask of <see cref="DeviceState"/> values to include.</param>
        public MMDeviceCollection EnumerateAudioEndPoints(DataFlow dataFlow, DeviceState dwStateMask)
        {
            CoreAudioException.ThrowIfFailed(realEnumerator.EnumAudioEndpoints(dataFlow, dwStateMask, out var ptr));
            try
            {
                return new MMDeviceCollection((IMMDeviceCollection)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                    ptr, CreateObjectFlags.UniqueInstance));
            }
            finally
            {
                Marshal.Release(ptr);
            }
        }

        /// <summary>
        /// Returns the default endpoint for the given direction and role.
        /// </summary>
        /// <exception cref="CoreAudioException">
        /// Thrown if no default endpoint exists. Use <see cref="HasDefaultAudioEndpoint"/> or
        /// <see cref="TryGetDefaultAudioEndpoint"/> for the non-throwing variants.
        /// </exception>
        public MMDevice GetDefaultAudioEndpoint(DataFlow dataFlow, Role role)
        {
            CoreAudioException.ThrowIfFailed(realEnumerator.GetDefaultAudioEndpoint(dataFlow, role, out var ptr));
            return WrapDevicePointer(ptr);
        }

        /// <summary>
        /// Returns <c>true</c> if a default endpoint exists for the given direction and role,
        /// without allocating a wrapper or throwing on the common "no default" case.
        /// </summary>
        public bool HasDefaultAudioEndpoint(DataFlow dataFlow, Role role)
        {
            const int E_NOTFOUND = unchecked((int)0x80070490);
            int hresult = realEnumerator.GetDefaultAudioEndpoint(dataFlow, role, out var ptr);
            if (hresult == 0x0)
            {
                Marshal.Release(ptr);
                return true;
            }
            if (hresult == E_NOTFOUND)
            {
                return false;
            }
            CoreAudioException.ThrowIfFailed(hresult);
            return false;
        }

        /// <summary>
        /// Attempts to resolve the default endpoint for the given direction and role.
        /// Returns <c>false</c> when no default exists, without throwing.
        /// </summary>
        /// <param name="dataFlow">Render or capture.</param>
        /// <param name="role">Console, multimedia, or communications role.</param>
        /// <param name="device">The default device on success; <c>null</c> otherwise.</param>
        public bool TryGetDefaultAudioEndpoint(DataFlow dataFlow, Role role, out MMDevice device)
        {
            const int E_NOTFOUND = unchecked((int)0x80070490);
            int hresult = realEnumerator.GetDefaultAudioEndpoint(dataFlow, role, out var ptr);
            if (hresult == 0x0)
            {
                device = WrapDevicePointer(ptr);
                return true;
            }
            device = null;
            if (hresult == E_NOTFOUND)
            {
                return false;
            }
            CoreAudioException.ThrowIfFailed(hresult);
            return false;
        }

        /// <summary>
        /// Resolves an endpoint by its <see cref="MMDevice.ID"/> string.
        /// </summary>
        public MMDevice GetDevice(string id)
        {
            CoreAudioException.ThrowIfFailed(realEnumerator.GetDevice(id, out var ptr));
            return WrapDevicePointer(ptr);
        }

        /// <summary>
        /// Wraps a fresh <c>IMMDevice*</c> from the enumerator into an <see cref="MMDevice"/>,
        /// projecting via ComWrappers (UniqueInstance) and releasing the input pointer.
        /// </summary>
        private static MMDevice WrapDevicePointer(IntPtr ptr)
        {
            try
            {
                return new MMDevice((IMMDevice)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                    ptr, CreateObjectFlags.UniqueInstance));
            }
            finally
            {
                Marshal.Release(ptr);
            }
        }

        /// <summary>
        /// Subscribes <paramref name="client"/> to endpoint change notifications
        /// (device added / removed / state change / default changed / property value change).
        /// </summary>
        /// <returns>The HRESULT from the underlying COM call.</returns>
        public int RegisterEndpointNotificationCallback(IMMNotificationClient client)
        {
            var ptr = ComActivation.ComWrappers.GetOrCreateComInterfaceForObject(client, CreateComInterfaceFlags.None);
            try
            {
                return realEnumerator.RegisterEndpointNotificationCallback(ptr);
            }
            finally
            {
                Marshal.Release(ptr);
            }
        }

        /// <summary>
        /// Removes a previously registered notification subscription.
        /// </summary>
        /// <returns>The HRESULT from the underlying COM call.</returns>
        public int UnregisterEndpointNotificationCallback(IMMNotificationClient client)
        {
            var ptr = ComActivation.ComWrappers.GetOrCreateComInterfaceForObject(client, CreateComInterfaceFlags.None);
            try
            {
                return realEnumerator.UnregisterEndpointNotificationCallback(ptr);
            }
            finally
            {
                Marshal.Release(ptr);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the underlying COM enumerator. Safe to call multiple times.
        /// </summary>
        /// <param name="disposing"><c>true</c> when called from <see cref="Dispose()"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (realEnumerator != null)
                {
                    if ((object)realEnumerator is ComObject co)
                    {
                        co.FinalRelease();
                    }
                    realEnumerator = null;
                }
            }
        }
    }
}
