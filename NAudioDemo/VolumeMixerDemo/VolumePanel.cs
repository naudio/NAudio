using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using System.Diagnostics;
using NAudio.CoreAudioApi.Interfaces;
using System.Media;

namespace NAudioDemo.VolumeMixerDemo
{
    public partial class VolumePanel : UserControl, IAudioSessionEventsHandler
    {
        private readonly bool devicePanel;
        private MMDevice device;
        private readonly AudioSessionControl session;

        public MMDevice Device
        {
            get
            {
                return device;
            }
        }

        public event EventHandler DeviceChanged;
        public event EventHandler<MuteEventArgs> MuteChanged;
        public event EventHandler<VolumeEventArgs> VolumeChanged;

        /// <summary>
        /// Constructor for device panel creation.
        /// </summary>
        public VolumePanel()
        {
            this.devicePanel = true;
            var deviceEnumerator = new MMDeviceEnumerator();
            device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            InitializeComponent();
        }

        /// <summary>
        /// Constructor for session panel creation.
        /// </summary>
        /// <param name="device">Selected device.</param>
        /// <param name="session">Current session of device.</param>
        public VolumePanel(MMDevice device, AudioSessionControl session)
        {
            this.devicePanel = false;
            this.device = device;
            this.session = session;
            InitializeComponent();

            cmbDevice.Visible = false;
            Process process = Process.GetProcessById((int)session.GetProcessID);
            if (session.IsSystemSoundsSession)
            {
                lblName.Text = "System Sounds";
                pbProcessIcon.Visible = false;
                btnSoundProperties.Visible = true;
                var iconAddress = session.IconPath.Split(',');
                var icon = IconExtractor.Extract(iconAddress[0], int.Parse(iconAddress[1]), true);
                if (icon != null)
                    btnSoundProperties.Image = icon.ToBitmap();
                tooltip.SetToolTip(btnSoundProperties, lblName.Text);
            }
            else
            {
                pbProcessIcon.Image = Icon.ExtractAssociatedIcon(process.MainModule.FileName).ToBitmap();
                lblName.Text = process.MainWindowTitle != "" ? process.MainWindowTitle : process.ProcessName;
                pbProcessIcon.Visible = true;
                btnSoundProperties.Visible = false;
                tooltip.SetToolTip(pbProcessIcon, lblName.Text);
            }
            tooltip.SetToolTip(lblName, lblName.Text);

            session.RegisterEventClient(this);
            UpdateVolume();
            UpdateMuted();
        }

        private void VolumePanel_Load(object sender, EventArgs e)
        {
            if (devicePanel)
            {
                cmbDevice.Visible = true;
                var deviceEnumerator = new MMDeviceEnumerator();
                foreach (var d in deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                {
                    cmbDevice.Items.Add(d);
                    if (d.ID == device.ID)
                    {
                        cmbDevice.SelectedItem = d;
                        d.AudioEndpointVolume.OnVolumeNotification += AudioEndpointVolume_OnVolumeNotification;
                    }
                }
            }

            UpdateVolume();
            UpdateMuted();
        }

        private void tbVolume_Scroll(object sender, EventArgs e)
        {
            float volume;
            volume = tbVolume.Value / 100.0f;
            if (!devicePanel)
            {
                var newVolume = volume / Device.AudioEndpointVolume.MasterVolumeLevelScalar;
                if (newVolume <= 1)
                    session.SimpleAudioVolume.Volume = newVolume;
                else
                {
                    Device.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
                    session.SimpleAudioVolume.Volume = 1;
                }
                session.SimpleAudioVolume.Mute = false;
            }
            else
            {
                Device.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
            }
            Device.AudioEndpointVolume.Mute = false;
            if (VolumeChanged != null)
                VolumeChanged(sender, new VolumeEventArgs(volume));
            UpdateMuted();
            if (MuteChanged != null)
                MuteChanged(sender, new MuteEventArgs(false));
        }

        private void btnMuteUnmute_Click(object sender, EventArgs e)
        {
            bool muted;
            if (!devicePanel)
            {
                muted = !session.SimpleAudioVolume.Mute;
                session.SimpleAudioVolume.Mute = muted;
                if (!muted)
                    Device.AudioEndpointVolume.Mute = false;
            }
            else
            {
                muted = !Device.AudioEndpointVolume.Mute;
                Device.AudioEndpointVolume.Mute = muted;
            }
            UpdateMuted();
            if (MuteChanged != null)
                MuteChanged(sender, new MuteEventArgs(muted));
        }

        private void cmbDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            device = (MMDevice)cmbDevice.SelectedItem;
            if (DeviceChanged != null)
                DeviceChanged(sender, e);
            var tooltipText = cmbDevice.SelectedItem.ToString();
            tooltip.SetToolTip(cmbDevice, tooltipText);
            tooltip.SetToolTip(btnSoundProperties, tooltipText);
            var iconAddress = ((MMDevice)cmbDevice.SelectedItem).IconPath.Split(',');
            btnSoundProperties.Image = IconExtractor.Extract(iconAddress[0], int.Parse(iconAddress[1]), true).ToBitmap();
        }

        private void btnSoundProperties_Click(object sender, EventArgs e)
        {
            if (!devicePanel)
                System.Diagnostics.Process.Start("control", "mmsys.cpl,,2");
            else
                System.Diagnostics.Process.Start("control", "mmsys.cpl,,0");
        }

        private void tbVolume_MouseCaptureChanged(object sender, EventArgs e)
        {
            if (!devicePanel && session.IsSystemSoundsSession)
                SystemSounds.Beep.Play();
        }

        public void UpdateVolume()
        {
            if (!devicePanel)
            {
                float volume = session.SimpleAudioVolume.Volume;
                tbVolume.Value = (int)(Math.Round(volume * Device.AudioEndpointVolume.MasterVolumeLevelScalar * 100));
            }
            else
            {
                tbVolume.Value = (int)(Math.Round(Device.AudioEndpointVolume.MasterVolumeLevelScalar * 100));
                if (VolumeChanged != null)
                    VolumeChanged(null, new VolumeEventArgs(Device.AudioEndpointVolume.MasterVolumeLevelScalar));
            }
        }

        public void UpdateMuted()
        {
            bool mute;
            if (!devicePanel)
            {
                mute = session.SimpleAudioVolume.Mute;
                if (device.AudioEndpointVolume.Mute)
                    mute = true;
            }
            else
            {
                mute = device.AudioEndpointVolume.Mute;
                if (MuteChanged != null)
                    MuteChanged(null, new MuteEventArgs(Device.AudioEndpointVolume.Mute));
            }
            btnMuteUnmute.ImageKey = mute ? "Mute.png" : "Unmute.png";
        }

        void AudioEndpointVolume_OnVolumeNotification(AudioVolumeNotificationData data)
        {
            try
            {
                this.Invoke(new Action(delegate()
                {
                    UpdateVolume();
                    UpdateMuted();
                }));
            }
            catch
            {
            }
        }

        public void OnVolumeChanged(float volume, bool isMuted)
        {
            try
            {
                this.Invoke(new Action(delegate()
                {
                    UpdateVolume();
                    UpdateMuted();
                }));
            }
            catch
            {
            }
        }

        public void OnDisplayNameChanged(string displayName)
        {
            this.Invoke(new Action(delegate()
            {
                lblName.Text = displayName;
            }));
        }

        public void OnIconPathChanged(string iconPath)
        {
        }

        public void OnChannelVolumeChanged(uint channelCount, IntPtr newVolumes, uint channelIndex)
        {
        }

        public void OnGroupingParamChanged(ref Guid groupingId)
        {
        }

        public void OnStateChanged(AudioSessionState state)
        {
        }

        public void OnSessionDisconnected(AudioSessionDisconnectReason disconnectReason)
        {
        }
    }

    public class MuteEventArgs : EventArgs
    {
        public MuteEventArgs(bool muted)
        {
            Muted = muted;
        }

        public bool Muted { get; private set; }
    }

    public class VolumeEventArgs : EventArgs
    {
        public VolumeEventArgs(float volume)
        {
            Volume = volume;
        }

        public float Volume { get; private set; }
    }
}