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
            IWavePlayer device;
            var strategy = waveOutSettingsPanel.CallbackStrategy;
            if (strategy == WaveCallbackStrategy.Event)
            {
                var waveOut = new WaveOutEvent();
                waveOut.DeviceNumber = waveOutSettingsPanel.SelectedDeviceNumber;
                waveOut.DesiredLatency = latency;
                device = waveOut;
            }
            else
            {
                WaveCallbackInfo callbackInfo = strategy == WaveCallbackStrategy.NewWindow ? WaveCallbackInfo.NewWindow() : WaveCallbackInfo.FunctionCallback();
                WaveOut outputDevice = new WaveOut(callbackInfo);
                outputDevice.DeviceNumber = waveOutSettingsPanel.SelectedDeviceNumber;
                outputDevice.DesiredLatency = latency;
                device = outputDevice;
            }
            // TODO: configurable number of buffers

            return device;
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
            get { return 1; } 
        }
    }
}
