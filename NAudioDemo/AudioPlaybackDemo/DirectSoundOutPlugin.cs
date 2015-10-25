using System;
using System.Linq;
using NAudio.Wave;
using System.Windows.Forms;

namespace NAudioDemo.AudioPlaybackDemo
{
    class DirectSoundOutPlugin : IOutputDevicePlugin
    {
        private DirectSoundOutSettingsPanel settingsPanel;
        private readonly bool isAvailable;

        public DirectSoundOutPlugin()
        {
            isAvailable = DirectSoundOut.Devices.Any();
        }

        public IWavePlayer CreateDevice(int latency)
        {
            return new DirectSoundOut(settingsPanel.SelectedDevice, latency);
        }

        public UserControl CreateSettingsPanel()
        {
            settingsPanel = new DirectSoundOutSettingsPanel();
            return settingsPanel;
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
