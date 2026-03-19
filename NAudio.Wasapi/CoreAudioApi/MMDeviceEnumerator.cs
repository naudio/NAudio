/*
  LICENSE
  -------
  Copyright (C) 2007 Ray Molenkamp

  This source code is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this source code or the software it produces.

  Permission is granted to anyone to use this source code for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this source code must not be misrepresented; you must not
     claim that you wrote the original source code.  If you use this source code
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original source code.
  3. This notice may not be removed or altered from any source distribution.
*/
// updated for use in NAudio
using System;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudio.CoreAudioApi
{

    /// <summary>
    /// MM Device Enumerator
    /// </summary>
    public class MMDeviceEnumerator : IDisposable
    {
        private IMMDeviceEnumerator realEnumerator;

        /// <summary>
        /// Creates a new MM Device Enumerator
        /// </summary>
        public MMDeviceEnumerator()
        {
            if (Environment.OSVersion.Version.Major < 6)
            {
                throw new NotSupportedException("This functionality is only supported on Windows Vista or newer.");
            }
            // Activate the COM server via the [ComImport] coclass, then obtain the
            // [GeneratedComInterface] wrapper via QueryInterface on the raw pointer.
            var comObj = new MMDeviceEnumeratorComObject();
            var ptr = Marshal.GetIUnknownForObject(comObj);
            realEnumerator = (IMMDeviceEnumerator)Marshal.GetObjectForIUnknown(ptr);
            Marshal.Release(ptr);
        }

        /// <summary>
        /// Enumerate Audio Endpoints
        /// </summary>
        /// <param name="dataFlow">Desired DataFlow</param>
        /// <param name="dwStateMask">State Mask</param>
        /// <returns>Device Collection</returns>
        public MMDeviceCollection EnumerateAudioEndPoints(DataFlow dataFlow, DeviceState dwStateMask)
        {
            CoreAudioException.ThrowIfFailed(realEnumerator.EnumAudioEndpoints(dataFlow, dwStateMask, out var ptr));
            return new MMDeviceCollection((IMMDeviceCollection)Marshal.GetObjectForIUnknown(ptr));
        }

        /// <summary>
        /// Get Default Endpoint
        /// </summary>
        /// <param name="dataFlow">Data Flow</param>
        /// <param name="role">Role</param>
        /// <returns>Device</returns>
        public MMDevice GetDefaultAudioEndpoint(DataFlow dataFlow, Role role)
        {
            CoreAudioException.ThrowIfFailed(realEnumerator.GetDefaultAudioEndpoint(dataFlow, role, out var ptr));
            return new MMDevice((IMMDevice)Marshal.GetObjectForIUnknown(ptr));
        }

        /// <summary>
        /// Check to see if a default audio end point exists without needing an exception.
        /// </summary>
        /// <param name="dataFlow">Data Flow</param>
        /// <param name="role">Role</param>
        /// <returns>True if one exists, and false if one does not exist.</returns>
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
        /// Tries to get the default audio endpoint for the specified data flow and role.
        /// Returns false if no default endpoint exists, without throwing an exception.
        /// </summary>
        /// <param name="dataFlow">Data Flow</param>
        /// <param name="role">Role</param>
        /// <param name="device">The default device, or null if none exists.</param>
        /// <returns>True if a default endpoint was found.</returns>
        public bool TryGetDefaultAudioEndpoint(DataFlow dataFlow, Role role, out MMDevice device)
        {
            const int E_NOTFOUND = unchecked((int)0x80070490);
            int hresult = realEnumerator.GetDefaultAudioEndpoint(dataFlow, role, out var ptr);
            if (hresult == 0x0)
            {
                device = new MMDevice((IMMDevice)Marshal.GetObjectForIUnknown(ptr));
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
        /// Get device by ID
        /// </summary>
        /// <param name="id">Device ID</param>
        /// <returns>Device</returns>
        public MMDevice GetDevice(string id)
        {
            CoreAudioException.ThrowIfFailed(realEnumerator.GetDevice(id, out var ptr));
            return new MMDevice((IMMDevice)Marshal.GetObjectForIUnknown(ptr));
        }

        /// <summary>
        /// Registers a call back for Device Events
        /// </summary>
        /// <param name="client">Object implementing IMMNotificationClient type casted as IMMNotificationClient interface</param>
        /// <returns></returns>
        public int RegisterEndpointNotificationCallback(IMMNotificationClient client)
        {
            var ptr = Marshal.GetComInterfaceForObject(client, typeof(IMMNotificationClient));
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
        /// Unregisters a call back for Device Events
        /// </summary>
        /// <param name="client">Object implementing IMMNotificationClient type casted as IMMNotificationClient interface </param>
        /// <returns></returns>
        public int UnregisterEndpointNotificationCallback(IMMNotificationClient client)
        {
            var ptr = Marshal.GetComInterfaceForObject(client, typeof(IMMNotificationClient));
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
        /// Called to dispose/finalize contained objects.
        /// </summary>
        /// <param name="disposing">True if disposing, false if called from a finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                realEnumerator = null;
            }
        }
    }
}
