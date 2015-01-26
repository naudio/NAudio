using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;
using System.Windows.Forms;
using System.ComponentModel.Composition;

namespace NAudioDemo.AudioPlaybackDemo
{
    [Export(typeof(IOutputDevicePlugin))]
    class DirectSoundOutPlugin : IOutputDevicePlugin
    {
        private DirectSoundOutSettingsPanel settingsPanel;
        private bool isAvailable;

        public DirectSoundOutPlugin()
        {
            this.isAvailable = DirectSoundOut.Devices.Count() > 0;
        }

        public IWavePlayer CreateDevice(int latency)
        {
            return new DirectSoundOut(settingsPanel.SelectedDevice, latency);
        }

        public UserControl CreateSettingsPanel()
        {
            this.settingsPanel = new DirectSoundOutSettingsPanel();
            return this.settingsPanel;
        }

        public string Name
        {
            get { return "DirectSound"; }
        }

        public bool IsAvailable
        {
            get { return isAvailable; }
        }

        public int Priority
        {
            get { return 2; } 
        }
    }
}
