using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using System.Diagnostics;

namespace NAudioDemo.VolumeMixerDemo
{
    public partial class VolumeMixerPanel : UserControl
    {
        VolumePanel DeviceVolumePanel;
        List<VolumePanel> AppVolumePanels = new List<VolumePanel>();
        AudioSessionManager subscribedSessionManager;

        public VolumeMixerPanel()
        {
            InitializeComponent();

            btnUpdate_Click(null, null);
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            UnsubscribeFromSessionManager();
            DeviceVolumePanel = new VolumePanel();
            DeviceVolumePanel.DeviceChanged += DeviceVolumePanel_DeviceChanged;
            DeviceVolumePanel.MuteChanged += DeviceVolumePanel_MuteChanged;
            DeviceVolumePanel.VolumeChanged += DeviceVolumePanel_VolumeChanged;
            flowLayoutPanelDevice.Controls.Clear();
            flowLayoutPanelDevice.Controls.Add(DeviceVolumePanel);
        }

        void DeviceVolumePanel_VolumeChanged(object sender, VolumeEventArgs e)
        {
            foreach (var appVolumePanel in AppVolumePanels)
                appVolumePanel.UpdateVolume();
        }

        void DeviceVolumePanel_MuteChanged(object sender, MuteEventArgs e)
        {
            foreach (var appVolumePanel in AppVolumePanels)
                appVolumePanel.UpdateMuted();
        }

        void DeviceVolumePanel_DeviceChanged(object sender, object e)
        {
            UnsubscribeFromSessionManager();
            flowLayoutPanelApps.Controls.Clear();
            var device = (MMDevice)DeviceVolumePanel.Device;
            DeviceVolumePanel.UpdateVolume();

            var sessionManager = device.AudioSessionManager;
            var sessions = sessionManager.Sessions;
            if (sessions == null) return;

            // Exercises IAudioSessionNotification.OnSessionCreated. Without subscribing to
            // this event, the volume mixer would silently miss new sessions appearing —
            // and if the underlying CCW dispatch ever regresses, we'd have no signal here.
            sessionManager.OnSessionCreated += OnSessionCreated;
            subscribedSessionManager = sessionManager;

            AppVolumePanels = new List<VolumePanel>(sessions.Count);
            for (int i = 0; i < sessions.Count; i++)
            {
                var session = sessions[i];
                if (session.IsSystemSoundsSession && ProcessExists(session.GetProcessID))
                {
                    AddVolumePanel(session);
                    break;
                }
            }
            for (int i = 0; i < sessions.Count; i++)
            {
                var session = sessions[i];
                if (!session.IsSystemSoundsSession && ProcessExists(session.GetProcessID))
                    AddVolumePanel(session);
            }
        }

        void OnSessionCreated(object sender, AudioSessionControl newSession)
        {
            string display;
            try { display = newSession.IsSystemSoundsSession ? "System Sounds" : $"PID {newSession.GetProcessID}"; }
            catch { display = "<unknown>"; }
            Debug.WriteLine($"[VolumeMixer] OnSessionCreated fired ({display}) — IAudioSessionNotification CCW dispatch OK");

            if (InvokeRequired)
            {
                BeginInvoke(new EventHandler<AudioSessionControl>(OnSessionCreated), sender, newSession);
                return;
            }
            if (ProcessExists(newSession.GetProcessID))
            {
                AddVolumePanel(newSession);
            }
        }

        void UnsubscribeFromSessionManager()
        {
            if (subscribedSessionManager != null)
            {
                subscribedSessionManager.OnSessionCreated -= OnSessionCreated;
                subscribedSessionManager = null;
            }
        }

        bool ProcessExists(uint processId)
        {
            try
            {
                var process = Process.GetProcessById((int)processId);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        void AddVolumePanel(AudioSessionControl session)
        {
            var panel = new VolumePanel(DeviceVolumePanel.Device, session);
            AppVolumePanels.Add(panel);
            flowLayoutPanelApps.Controls.Add(panel);
        }
    }
}