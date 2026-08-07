
using System;
using System.Threading;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using NAudio.MacOS.CoreAudio.Interop;

namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// Provides an object that provides the property listener to managed clients. <br />
/// Each managed client that uses this class must dispose it's exposed listener after usage
/// by using the provided <see cref="Dispose"/> method.
/// </summary>
public sealed unsafe class PropertyListenerHandle : IDisposable
{
    // mdcdi1315: Instead of using a GC handle, use a concurrent dictionary
    // that manages the unmanaged->managed translations that also handles
    // the registrations in a thread-safe manner, which we first add the mapping
    // and then the doing the native call to define the handler.
    // By defining the handler as a managed object as the type of the value
    // in the mappings dictionary, we avoid completely using the GC handle API
    // and instead the client data parameter recieves the allocated handler ID
    // that is used in the below dictionary object to get the handler.
    // We can in fact do this because even on 32-bit platforms an IntPtr is an int value,
    // so it will safely work in terms of storage. In terms of memory management,
    // the HAL thinks that the passed in client data is an arbitrary pointer that it
    // does not have authority upon, so we are ok passing an arbitrary int value to it.
    private static int nextID = 0;
    private static readonly ConcurrentDictionary<int, Action<Span<AudioObjectPropertyAddress>>> handlerMappings = new();

    private int handlerId;
    private bool disposed;
    private readonly AudioObject audioObject;
    private readonly AudioObjectPropertyAddress address;
    private readonly AudioObjectPropertyListenerProc listener;

    internal PropertyListenerHandle(AudioObject obj, AudioObjectPropertyAddress address)
    {
        handlerId = 0;
        disposed = false;
        audioObject = obj;
        this.address = address;
        // The reason why we need a new listener address for this is
        // because this is a property listener and if it was a single
        // shared pointer, it would unregister all the other property
        // listeners on the same AudioObjectPropertyAddress.
        listener = &EventHandlerNativeMethodDecl;
        Event = new(DummyEventHandler);
        AddNotificationHandler();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int EventHandlerNativeMethodDecl(AudioObjectID id, uint nAddresses, IntPtr addresses, IntPtr clientData)
    {
        if (handlerMappings.TryGetValue(clientData.ToInt32(), out var action))
        {
            try
            {
                action.Invoke(new(addresses.ToPointer(), checked((int)nAddresses)));
            }
            catch (Exception ex)
            {
                // We cannot do anything if the event has code that throws, 
                // the only thing that we will do is to report this on debug so that we (and the users using the debug assembly) know it.
                Debug.WriteLine("[PropertyListenerHandle] Event invocation threw an exception: " + ex);
            }
        }
        return 0;
    }

    private static void DummyEventHandler(AudioObject o) { }

    private void FireEventNative(Span<AudioObjectPropertyAddress> addresses)
    {
        foreach (var a in addresses)
        {
            if (a == address)
            {
                Event.Invoke(audioObject);
                break;
            }
        }
    }

    // mdcdi1315: The below two methods handle the reg/unreg 
    // of the notification handler to the native side.
    // However, if deemeed necessary in the future
    // due to the fact that some listeners must be 
    // re-established after service restart or 
    // any update that the HAL instructs to do,
    // these two can be used to inject to a cache 
    // invalidated event (such as the service restarted event)
    // and mechanically remove and add again the notification handler.

    private void AddNotificationHandler()
    {
        int currentId = Interlocked.Add(ref nextID, 1);
        if (!handlerMappings.TryAdd(currentId, new(FireEventNative)))
        {
            throw new InvalidOperationException(
                $"Attempted to add a handler for which the ID {currentId} is already occupied!"
            );
        }
        AudioObject.ThrowOnStatusError(
            address,
            NativeMethods.AudioObjectAddPropertyListener(
                audioObject.objectId,
                address,
                listener,
                handlerId = currentId
            )
        );
    }

    private void RemoveNotificationHandler()
    {
        // First try to remove the mapping, then the listener registration itself.
        _ = handlerMappings.TryRemove(handlerId, out _);
        AudioObject.ThrowOnStatusError(
            address,
            NativeMethods.AudioObjectRemovePropertyListener(
                audioObject.objectId,
                address,
                listener,
                handlerId
            )
        );
    }

    /// <summary>
    /// Gets the <see cref="AudioObject"/> instance where this property listener handle was created from.
    /// </summary>
    public AudioObject OriginatingObject => audioObject;

    /// <summary>
    /// Event that fires when the property attached to this event changes state.
    /// </summary>
    public event AudioObjectPropertyListenerDelegate Event;

    /// <summary>
    /// Frees this <see cref="PropertyListenerHandle"/> instance. <br />
    /// Thread-safe.
    /// </summary>
    /// <remarks>
    /// Note that instances of this class retain a shared ID
    /// of the listener that needs to be disposed of. <br />
    /// If you lose access to the instance, that means that
    /// you leak that registration along with the native
    /// property listener registration.
    /// </remarks>
    public void Dispose()
    {
        Monitor.Enter(this);
        try
        {
            if (!disposed)
            {
                RemoveNotificationHandler();
                disposed = true;
            }
        }
        finally
        {
            Monitor.Exit(this);
        }
    }
}