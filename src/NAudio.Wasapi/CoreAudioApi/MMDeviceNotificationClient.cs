using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudio.CoreAudioApi;

/// <summary>
/// High-level, event-based subscription to Windows Core Audio endpoint notifications. Obtain one from
/// <see cref="MMDeviceEnumerator.CreateNotificationClient"/>, subscribe to the events you care about,
/// and <see cref="Dispose"/> it (or the owning enumerator) when done.
/// </summary>
/// <remarks>
/// <para>
/// This is the recommended way to react to device add/remove, state changes, default-device changes,
/// and property-value changes. It wraps the raw Core Audio COM callback interface internally, so callers
/// never need to implement a <c>[GeneratedComClass]</c>, enable unsafe code, or manage the CCW lifetime
/// themselves.
/// </para>
/// <para>
/// By default the events are marshalled to the <see cref="SynchronizationContext"/> captured when the
/// client was created (e.g. the UI thread of a WinForms/WPF app), so handlers can touch UI state directly.
/// Pass <c>useSynchronizationContext: false</c> to receive events synchronously on the Windows audio
/// worker thread instead — in which case handlers must be nonblocking and must not call back into the
/// audio stack (e.g. disposing a player/recorder), which risks a deadlock.
/// </para>
/// </remarks>
public sealed partial class MMDeviceNotificationClient : IDisposable
{
    private readonly MMDeviceEnumerator enumerator;
    private readonly SynchronizationContext syncContext;
    private readonly NotificationCallback callback;
    private bool disposed;

    /// <summary>
    /// Raised when the state of an endpoint device changes (active, disabled, not-present, unplugged).
    /// </summary>
    public event EventHandler<DeviceStateChangedEventArgs> DeviceStateChanged;

    /// <summary>
    /// Raised when a new endpoint device is added.
    /// </summary>
    public event EventHandler<DeviceNotificationEventArgs> DeviceAdded;

    /// <summary>
    /// Raised when an endpoint device is removed.
    /// </summary>
    public event EventHandler<DeviceNotificationEventArgs> DeviceRemoved;

    /// <summary>
    /// Raised when the default endpoint device for a given data-flow direction and role changes.
    /// </summary>
    public event EventHandler<DefaultDeviceChangedEventArgs> DefaultDeviceChanged;

    /// <summary>
    /// Raised when a property value of an endpoint device changes. This can be high-frequency (for
    /// example a volume change fires it), so keep handlers cheap.
    /// </summary>
    public event EventHandler<DevicePropertyChangedEventArgs> PropertyValueChanged;

    internal MMDeviceNotificationClient(MMDeviceEnumerator enumerator, bool useSynchronizationContext)
    {
        this.enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));
        syncContext = useSynchronizationContext ? SynchronizationContext.Current : null;
        callback = new NotificationCallback(this);
        // The enumerator retains the CCW reference for the registration's lifetime, so the callback
        // (and therefore this client) stays alive until Dispose even if the caller drops the reference.
        Marshal.ThrowExceptionForHR(enumerator.RegisterEndpointNotificationCallback(callback));
    }

    /// <summary>
    /// Unregisters from endpoint notifications. Safe to call multiple times. Disposing the owning
    /// <see cref="MMDeviceEnumerator"/> also releases the registration.
    /// </summary>
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { enumerator.UnregisterEndpointNotificationCallback(callback); }
        catch { /* enumerator may already be disposed */ }
    }

    private void Raise<T>(EventHandler<T> handler, T args) where T : EventArgs
    {
        if (handler is null) return;
        if (syncContext != null)
            syncContext.Post(_ => handler(this, args), null);
        else
            handler(this, args);
    }

    // Called by the internal CCW adapter on the audio worker thread.
    private void OnDeviceStateChanged(string deviceId, DeviceState newState)
        => Raise(DeviceStateChanged, new DeviceStateChangedEventArgs(deviceId, newState));

    private void OnDeviceAdded(string deviceId)
        => Raise(DeviceAdded, new DeviceNotificationEventArgs(deviceId));

    private void OnDeviceRemoved(string deviceId)
        => Raise(DeviceRemoved, new DeviceNotificationEventArgs(deviceId));

    private void OnDefaultDeviceChanged(DataFlow flow, Role role, string deviceId)
        => Raise(DefaultDeviceChanged, new DefaultDeviceChangedEventArgs(flow, role, deviceId));

    private void OnPropertyValueChanged(string deviceId, PropertyKey key)
        => Raise(PropertyValueChanged, new DevicePropertyChangedEventArgs(deviceId, key));

    // The single raw-COM callback implementation, kept internal so the COM boundary never leaks into
    // consuming code. Forwards each notification to the owning client's event dispatch.
    [GeneratedComClass]
    private sealed partial class NotificationCallback : IMMNotificationClient
    {
        private readonly MMDeviceNotificationClient owner;

        public NotificationCallback(MMDeviceNotificationClient owner) => this.owner = owner;

        public void OnDeviceStateChanged(string deviceId, DeviceState newState) => owner.OnDeviceStateChanged(deviceId, newState);
        public void OnDeviceAdded(string pwstrDeviceId) => owner.OnDeviceAdded(pwstrDeviceId);
        public void OnDeviceRemoved(string deviceId) => owner.OnDeviceRemoved(deviceId);
        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId) => owner.OnDefaultDeviceChanged(flow, role, defaultDeviceId);
        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) => owner.OnPropertyValueChanged(pwstrDeviceId, key);
    }
}
