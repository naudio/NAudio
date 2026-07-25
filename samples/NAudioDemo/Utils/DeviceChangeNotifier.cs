using System;
using NAudio.CoreAudioApi;

namespace NAudioDemo.Utils;

// Shared helper that raises a single .NET event whenever a relevant endpoint notification fires —
// device add/remove/state-change/default-change. Built on the high-level MMDeviceNotificationClient,
// which already marshals events to the calling thread's SynchronizationContext, so panels can refresh
// their device combos without worrying about WASAPI worker threads.
//
// Used by RecordingPanel and the AudioPlaybackDemo's WASAPI settings panel to auto-repopulate their
// device combos.
internal sealed class DeviceChangeNotifier : IDisposable
{
    private readonly MMDeviceNotificationClient notifications;

    public event Action DevicesChanged;

    public DeviceChangeNotifier(MMDeviceEnumerator enumerator)
    {
        notifications = enumerator.CreateNotificationClient();
        notifications.DeviceAdded += OnChanged;
        notifications.DeviceRemoved += OnChanged;
        notifications.DeviceStateChanged += OnChanged;
        notifications.DefaultDeviceChanged += OnChanged;
        // Property-value changes are high frequency and irrelevant to a device-list refresh, so ignore them.
    }

    private void OnChanged(object sender, EventArgs e) => DevicesChanged?.Invoke();

    public void Dispose() => notifications.Dispose();
}
