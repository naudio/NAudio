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
    class AsioOutPlugin : IOutputDevicePlugin
    {
        AsioOutSettingsPanel settingsPanel;

        public IWavePlayer CreateDevice(int latency)
        {
            return new AsioOut(settingsPanel.SelectedDeviceName);
        }

        public UserControl CreateSettingsPanel()
        {
            this.settingsPanel = new AsioOutSettingsPanel();
            return settingsPanel;
        }

        public string Name
        {
            get { return "AsioOut"; }
        }

        public bool IsAvailable
        {
            get { return AsioOut.isSupported(); }
        }

        public int Priority
        {
            get { return 4; }
        }
    }
}
