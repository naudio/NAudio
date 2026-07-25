using System;
using System.Linq;
using System.Windows.Forms;
using NAudio.CoreAudioApi;

namespace NAudioDemo.DeviceNotificationsDemo;

// Exercises the high-level MMDeviceNotificationClient event API — the recommended way to observe
// endpoint changes. Subscribes once and dumps every notification to a listbox, so plugging/unplugging
// USB audio devices, switching defaults in the system Sound panel, or muting via the system tray each
// produce a visible event row. Events arrive on the captured SynchronizationContext (the UI thread),
// so handlers update the UI directly.
public partial class DeviceNotificationsPanel : UserControl
{
    private readonly MMDeviceEnumerator enumerator = new();
    private readonly MMDeviceNotificationClient notifications;

    public DeviceNotificationsPanel()
    {
        InitializeComponent();
        notifications = enumerator.CreateNotificationClient();
        notifications.DeviceStateChanged += (_, e) => { Log(nameof(notifications.DeviceStateChanged), $"{e.NewState}  {e.DeviceId}"); RefreshDeviceList(); };
        notifications.DeviceAdded += (_, e) => { Log(nameof(notifications.DeviceAdded), e.DeviceId); RefreshDeviceList(); };
        notifications.DeviceRemoved += (_, e) => { Log(nameof(notifications.DeviceRemoved), e.DeviceId); RefreshDeviceList(); };
        notifications.DefaultDeviceChanged += (_, e) => Log(nameof(notifications.DefaultDeviceChanged), $"{e.Flow}/{e.Role} → {e.DeviceId ?? "<none>"}");
        notifications.PropertyValueChanged += (_, e) => Log(nameof(notifications.PropertyValueChanged), $"{e.PropertyKey.formatId}/{e.PropertyKey.propertyId}  {e.DeviceId}");
        Disposed += OnDisposed;
        RefreshDeviceList();
    }

    private void OnDisposed(object sender, EventArgs e)
    {
        notifications.Dispose();
        enumerator.Dispose();
    }

    private void RefreshDeviceList()
    {
        listDevices.Items.Clear();
        foreach (var d in enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.All)
                                    .OrderBy(d => d.DataFlow.ToString())
                                    .ThenBy(d => d.FriendlyName))
        {
            listDevices.Items.Add($"{d.DataFlow,-8}  {d.State,-12}  {d.FriendlyName}");
        }
    }

    private void Log(string method, string body)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff}  {method,-25}  {body}";
        listEvents.Items.Insert(0, line);
        if (listEvents.Items.Count > 500) listEvents.Items.RemoveAt(listEvents.Items.Count - 1);
    }

    private void OnClearClick(object sender, EventArgs e) => listEvents.Items.Clear();
}

public class DeviceNotificationsPanelPlugin : INAudioDemoPlugin
{
    public string Name => "Device Notifications";
    public Control CreatePanel() => new DeviceNotificationsPanel();
}
