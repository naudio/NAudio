using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using System.Diagnostics;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudioDemo.VolumeMixerDemo
{
    /// <summary>
    /// TODO: Sessions create and dispose events are not handled.
    /// </summary>
    public partial class VolumeMixerPanel : UserControl
    {
        VolumePanel DeviceVolumePanel;
        List<VolumePanel> AppVolumePanels = new List<VolumePanel>();

        public VolumeMixerPanel()
        {
            InitializeComponent();

            btnUpdate_Click(null, null);
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
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
            flowLayoutPanelApps.Controls.Clear();
            var device = (MMDevice)DeviceVolumePanel.Device;
            DeviceVolumePanel.UpdateVolume();
            
            var sessions = device.AudioSessionManager.Sessions;
            if (sessions == null) return;
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