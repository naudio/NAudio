using NAudio.Wave;
using System.Windows.Forms;

namespace NAudioDemo.AudioPlaybackDemo
{
    class WaveOutWindowPlugin : IOutputDevicePlugin
    {
        private WaveOutSettingsPanel waveOutSettingsPanel;

        public IWavePlayer CreateDevice(int latency)
        {
            return new WaveOutWindow
            {
                DeviceNumber = waveOutSettingsPanel.SelectedDeviceNumber,
                DesiredLatency = latency,
            };
        }

        public UserControl CreateSettingsPanel()
        {
            waveOutSettingsPanel = new WaveOutSettingsPanel();
            return waveOutSettingsPanel;
        }

        public string Name => "WaveOutWindow";

        public bool IsAvailable => WaveOutWindow.DeviceCount > 0;

        public int Priority => 5;
    }
}
