using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudio.CoreAudioApi;

/// <summary>
/// Entry point to the Windows Core Audio device enumeration API
/// (<c>IMMDeviceEnumerator</c>). Use this to discover audio endpoints,
/// resolve the default render or capture device, look up a device by ID,
/// and subscribe to endpoint change notifications.
/// </summary>
public class MMDeviceEnumerator : IDisposable
{
    // CLSID_MMDeviceEnumerator — mmdeviceapi.h
    private static readonly Guid CLSID_MMDeviceEnumerator = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    // IID_IMMDeviceEnumerator — mmdeviceapi.h
    private static readonly Guid IID_IMMDeviceEnumerator = new("A95664D2-9614-4F35-A746-DE8DB63617E6");
    // IID_IMMNotificationClient — mmdeviceapi.h
    private static readonly Guid IID_IMMNotificationClient = new("7991EEC9-7E89-4D85-8390-6C703CEC60C0");

    private IMMDeviceEnumerator realEnumerator;

    // Registered notification clients, keyed by the managed client (reference identity) with the
    // retained CCW interface pointer as the value. Windows does NOT AddRef the client passed to
    // RegisterEndpointNotificationCallback (see that method's remarks), so if we released the CCW
    // reference we handed to native, the CCW's ref count would hit zero, ComWrappers would demote
    // its handle to the managed client to weak, and the next GC would collect the client. Native
    // would then dispatch a notification into a dead object -> AccessViolationException in
    // ComInterfaceDispatch.GetInstance. Holding the CCW reference here keeps the ref count > 0
    // (which keeps the managed client strongly rooted) until the client is unregistered/disposed.
    private readonly Dictionary<IMMNotificationClient, IntPtr> registeredClients = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Activates the system <c>MMDeviceEnumerator</c> COM object.
    /// </summary>
    public MMDeviceEnumerator()
    {
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
    /// <remarks>
    /// The client's callbacks run on a Windows audio system worker thread that holds an
    /// internal lock for the duration of the call. Handlers must not block, wait on another
    /// thread, or call back into the audio stack (e.g. disposing a player/recorder); doing so
    /// risks a deadlock. Marshal any reaction to your own thread asynchronously
    /// (<c>BeginInvoke</c>, a queued <c>Task.Run</c>, etc.). See <see cref="IMMNotificationClient"/>.
    /// </remarks>
    /// <returns>The HRESULT from the underlying COM call.</returns>
    public int RegisterEndpointNotificationCallback(IMMNotificationClient client)
    {
        if (client is null) throw new ArgumentNullException(nameof(client));
        var ptr = QueryNotificationClientInterface(client);
        int hresult = realEnumerator.RegisterEndpointNotificationCallback(ptr);
        if (hresult >= 0)
        {
            // Success: retain the CCW reference for the whole registration (see registeredClients).
            // A re-registration of the same client replaces the retained reference; drop the stale one.
            if (registeredClients.Remove(client, out var stale))
            {
                Marshal.Release(stale);
            }
            registeredClients[client] = ptr;
        }
        else
        {
            // Failed to register: nothing to keep alive, release the reference we created.
            Marshal.Release(ptr);
        }
        return hresult;
    }

    /// <summary>
    /// Removes a previously registered notification subscription.
    /// </summary>
    /// <returns>The HRESULT from the underlying COM call.</returns>
    public int UnregisterEndpointNotificationCallback(IMMNotificationClient client)
    {
        if (client is null) throw new ArgumentNullException(nameof(client));
        var ptr = QueryNotificationClientInterface(client);
        try
        {
            int hresult = realEnumerator.UnregisterEndpointNotificationCallback(ptr);
            // Release the reference we retained at registration so the CCW ref count can fall to
            // zero and the managed client become collectable again.
            if (registeredClients.Remove(client, out var retained))
            {
                Marshal.Release(retained);
            }
            return hresult;
        }
        finally
        {
            Marshal.Release(ptr);
        }
    }

    // ComWrappers CCWs return a distinct IntPtr per interface (and a separate vtable
    // for IUnknown). RegisterEndpointNotificationCallback expects an IMMNotificationClient*,
    // so QI for the specific IID instead of passing the IUnknown CCW pointer directly.
    private static IntPtr QueryNotificationClientInterface(IMMNotificationClient client)
    {
        var unknownPtr = ComActivation.ComWrappers.GetOrCreateComInterfaceForObject(client, CreateComInterfaceFlags.None);
        try
        {
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface(unknownPtr, in IID_IMMNotificationClient, out var ifacePtr));
            return ifacePtr;
        }
        finally
        {
            Marshal.Release(unknownPtr);
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
            // Release the enumerator first so it can no longer dispatch notifications, then drop any
            // CCW references we retained for clients that were never explicitly unregistered.
            if (realEnumerator != null)
            {
                if ((object)realEnumerator is ComObject co)
                {
                    co.FinalRelease();
                }
                realEnumerator = null;
            }
            if (registeredClients.Count > 0)
            {
                foreach (var ptr in registeredClients.Values)
                {
                    Marshal.Release(ptr);
                }
                registeredClients.Clear();
            }
        }
    }
}
