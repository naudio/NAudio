using System;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudioDemo.DeviceNotificationsDemo
{
    // Exercises the public IMMNotificationClient interface — the only Phase 2f
    // callback users implement directly. Subscribes once via the COM-side
    // RegisterEndpointNotificationCallback and dumps every notification to a
    // listbox, so plugging/unplugging USB audio devices, switching defaults in
    // the system Sound panel, or muting via the system tray each produce a
    // visible event row. If a regression silently breaks the CCW dispatch this
    // panel goes quiet.
    public partial class DeviceNotificationsPanel : UserControl
    {
        private readonly MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
        private readonly NotificationClient client;

        public DeviceNotificationsPanel()
        {
            InitializeComponent();
            client = new NotificationClient(this);
            enumerator.RegisterEndpointNotificationCallback(client);
            Disposed += OnDisposed;
            RefreshDeviceList();
        }

        private void OnDisposed(object sender, EventArgs e)
        {
            try { enumerator.UnregisterEndpointNotificationCallback(client); }
            catch { /* best effort */ }
            enumerator.Dispose();
        }

        private void RefreshDeviceList()
        {
            if (InvokeRequired) { BeginInvoke(new Action(RefreshDeviceList)); return; }
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
            if (InvokeRequired) { BeginInvoke(new Action<string, string>(Log), method, body); return; }
            var line = $"{DateTime.Now:HH:mm:ss.fff}  {method,-25}  {body}";
            listEvents.Items.Insert(0, line);
            if (listEvents.Items.Count > 500) listEvents.Items.RemoveAt(listEvents.Items.Count - 1);
            // Refresh the device snapshot for events that change the visible state.
            switch (method)
            {
                case nameof(IMMNotificationClient.OnDeviceAdded):
                case nameof(IMMNotificationClient.OnDeviceRemoved):
                case nameof(IMMNotificationClient.OnDeviceStateChanged):
                    RefreshDeviceList();
                    break;
            }
        }

        private void OnClearClick(object sender, EventArgs e) => listEvents.Items.Clear();

        [GeneratedComClass]
        private partial class NotificationClient : IMMNotificationClient
        {
            private readonly DeviceNotificationsPanel parent;
            public NotificationClient(DeviceNotificationsPanel parent) => this.parent = parent;

            public void OnDeviceStateChanged(string deviceId, DeviceState newState) =>
                parent.Log(nameof(OnDeviceStateChanged), $"{newState}  {deviceId}");

            public void OnDeviceAdded(string pwstrDeviceId) =>
                parent.Log(nameof(OnDeviceAdded), pwstrDeviceId);

            public void OnDeviceRemoved(string deviceId) =>
                parent.Log(nameof(OnDeviceRemoved), deviceId);

            public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId) =>
                parent.Log(nameof(OnDefaultDeviceChanged), $"{flow}/{role} → {defaultDeviceId ?? "<none>"}");

            public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) =>
                parent.Log(nameof(OnPropertyValueChanged), $"{key.formatId}/{key.propertyId}  {pwstrDeviceId}");
        }
    }

    public class DeviceNotificationsPanelPlugin : INAudioDemoPlugin
    {
        public string Name => "Device Notifications";
        public Control CreatePanel() => new DeviceNotificationsPanel();
    }
}
