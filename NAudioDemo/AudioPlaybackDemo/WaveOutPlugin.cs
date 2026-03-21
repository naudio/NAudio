using System;
using System.Linq;
using NAudio.Wave;
using System.Windows.Forms;

namespace NAudioDemo.AudioPlaybackDemo
{
    class WaveOutPlugin : IOutputDevicePlugin
    {
        private WaveOutSettingsPanel waveOutSettingsPanel;

        public IWavePlayer CreateDevice(int latency)
        {
            var waveOut = new WaveOut();
            waveOut.DeviceNumber = waveOutSettingsPanel.SelectedDeviceNumber;
            waveOut.BufferMilliseconds = latency;
            return waveOut;
        }

        public UserControl CreateSettingsPanel()
        {
            waveOutSettingsPanel = new WaveOutSettingsPanel();
            return waveOutSettingsPanel;
        }

        public string Name
        {
            get { return "WaveOut"; }
        }

        public bool IsAvailable
        {
            get { return WaveOut.DeviceCount > 0; }
        }

        public int Priority
        {
            get { return 2; } 
        }
    }
}
