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
#if !NETFX_CORE
            if (System.Environment.OSVersion.Version.Major < 6)
            {
                throw new NotSupportedException("This functionality is only supported on Windows Vista or newer.");
            }
#endif
            realEnumerator = new MMDeviceEnumeratorComObject() as IMMDeviceEnumerator;
        }

        /// <summary>
        /// Enumerate Audio Endpoints
        /// </summary>
        /// <param name="dataFlow">Desired DataFlow</param>
        /// <param name="dwStateMask">State Mask</param>
        /// <returns>Device Collection</returns>
        public MMDeviceCollection EnumerateAudioEndPoints(DataFlow dataFlow, DeviceState dwStateMask)
        {
            Marshal.ThrowExceptionForHR(realEnumerator.EnumAudioEndpoints(dataFlow, dwStateMask, out var result));
            return new MMDeviceCollection(result);
        }

        /// <summary>
        /// Get Default Endpoint
        /// </summary>
        /// <param name="dataFlow">Data Flow</param>
        /// <param name="role">Role</param>
        /// <returns>Device</returns>
        public MMDevice GetDefaultAudioEndpoint(DataFlow dataFlow, Role role)
        {
            Marshal.ThrowExceptionForHR(((IMMDeviceEnumerator)realEnumerator).GetDefaultAudioEndpoint(dataFlow, role, out var device));
            return new MMDevice(device);
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
            int hresult = ((IMMDeviceEnumerator)realEnumerator).GetDefaultAudioEndpoint(dataFlow, role, out var device);
            if (hresult == 0x0)
            {
                Marshal.ReleaseComObject(device);
                return true;
            }
            if (hresult == E_NOTFOUND)
            {
                return false;
            }
            Marshal.ThrowExceptionForHR(hresult);
            return false;
        }

        /// <summary>
        /// Get device by ID
        /// </summary>
        /// <param name="id">Device ID</param>
        /// <returns>Device</returns>
        public MMDevice GetDevice(string id)
        {
            Marshal.ThrowExceptionForHR(((IMMDeviceEnumerator)realEnumerator).GetDevice(id, out var device));
            return new MMDevice(device);
        }

        /// <summary>
        /// Registers a call back for Device Events
        /// </summary>
        /// <param name="client">Object implementing IMMNotificationClient type casted as IMMNotificationClient interface</param>
        /// <returns></returns>
        public int RegisterEndpointNotificationCallback([In] [MarshalAs(UnmanagedType.Interface)] IMMNotificationClient client)
        {
            return realEnumerator.RegisterEndpointNotificationCallback(client);
        }

        /// <summary>
        /// Unregisters a call back for Device Events
        /// </summary>
        /// <param name="client">Object implementing IMMNotificationClient type casted as IMMNotificationClient interface </param>
        /// <returns></returns>
        public int UnregisterEndpointNotificationCallback([In] [MarshalAs(UnmanagedType.Interface)] IMMNotificationClient client)
        {
            return realEnumerator.UnregisterEndpointNotificationCallback(client);
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
                if (realEnumerator != null)
                {
                    // although GC would do this for us, we want it done now
                    Marshal.ReleaseComObject(realEnumerator);
                    realEnumerator = null;
                }
            }
        }
    }
}
