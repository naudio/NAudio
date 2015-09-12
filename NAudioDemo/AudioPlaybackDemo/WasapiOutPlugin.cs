using System;
using System.Linq;
using NAudio.Wave;
using System.Windows.Forms;

namespace NAudioDemo.AudioPlaybackDemo
{
    class WasapiOutPlugin : IOutputDevicePlugin
    {
        WasapiOutSettingsPanel settingsPanel;

        public IWavePlayer CreateDevice(int latency)
        {
            var wasapi = new WasapiOut(
                settingsPanel.SelectedDevice,
                settingsPanel.ShareMode,
                settingsPanel.UseEventCallback,
                latency);
            return wasapi;
        }

        public UserControl CreateSettingsPanel()
        {
            settingsPanel = new WasapiOutSettingsPanel();
            return settingsPanel;
        }

        public string Name
        {
            get { return "WasapiOut"; }
        }

        public bool IsAvailable
        {
            // supported on Vista and above
            get { return Environment.OSVersion.Version.Major >= 6; }
        }

        public int Priority
        {
            get { return 3; }
        }
    }
}
