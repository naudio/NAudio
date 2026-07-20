
using System;
using System.Threading;
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
    private readonly AudioObject audioObject;
    private readonly GCHandle delegateGcHandle;
    private readonly AudioObjectPropertyAddress address;
    private readonly AudioObjectPropertyListenerProc listener;

    internal PropertyListenerHandle(AudioObject obj, AudioObjectPropertyAddress address)
    {
        // The below handle retains a reference to the FireEventNative 
        // method that is invoked from the native side to achieve the native -> managed translation.
        // This method handle only retains the method itself, and the 'this' reference, required
        // to actually invoke the event.
        // This method also must accept a span of the addresses that this listener listens to.
        // This happens because each HAL listener may be registered to handful of properties,
        // even if the user wanted for actually one of them, so we verify the event dispatch 
        // on the FireEventNative method.
        delegateGcHandle = GCHandle.Alloc(new Action<Span<AudioObjectPropertyAddress>>(FireEventNative), GCHandleType.Normal);
        audioObject = obj;
        this.address = address;
        listener = &EventHandlerNativeMethodDecl;
        try
        {
            AddNotificationHandler();
        }
        catch
        {
            // If an exception occurrs during notification handler registration, we must dispose the allocated GC handle,
            // otherwise it will be leaked.
            delegateGcHandle.Free();
            throw;
        }
        Event = new(DummyEventHandler);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int EventHandlerNativeMethodDecl(AudioObjectID id, uint nAddresses, IntPtr addresses, IntPtr clientData)
    {
        ((Action<Span<AudioObjectPropertyAddress>>)GCHandle.FromIntPtr(clientData).Target)
            .Invoke(new(addresses.ToPointer(), checked((int)nAddresses)));
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
        AudioObject.ThrowOnStatusError(
            address,
            NativeMethods.AudioObjectAddPropertyListener(
                audioObject.objectId,
                address,
                listener,
                GCHandle.ToIntPtr(delegateGcHandle)
            )
        );
    }

    private void RemoveNotificationHandler()
    {
        AudioObject.ThrowOnStatusError(
            address,
            NativeMethods.AudioObjectRemovePropertyListener(
                audioObject.objectId,
                address,
                listener,
                GCHandle.ToIntPtr(delegateGcHandle)
            )
        );
    }

    /// <summary>
    /// Gets the <see cref="AudioObject"/> instance where this property listener handle was created from.
    /// </summary>
    public AudioObject OriginatingObject => audioObject;

    /// <summary>
    /// Event that fires when the property attached to this event changes state. <br />
    /// Note however that the HAL does not guarantee that the value of the property
    /// that this event is associated with will have been actually changed. HAL
    /// explicitly mentions that the actual property value update will happen after
    /// all property listeners have been invoked and completed execution.
    /// </summary>
    public event AudioObjectPropertyListenerDelegate Event;

    /// <summary>
    /// Frees this <see cref="PropertyListenerHandle"/> instance. <br />
    /// Thread-safe.
    /// </summary>
    public void Dispose()
    {
        Monitor.Enter(this);
        try
        {
            if (delegateGcHandle.IsAllocated)
            {
                try
                {
                    RemoveNotificationHandler();
                }
                finally
                {
                    // In either failure or success, the GC handle must be disposed of.
                    delegateGcHandle.Free();
                }
            }
        }
        finally
        {
            Monitor.Exit(this);
        }
    }
}