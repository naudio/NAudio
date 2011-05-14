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
    class WaveOutPlugin : IOutputDevicePlugin
    {
        private WaveOutSettingsPanel waveOutSettingsPanel;

        public IWavePlayer CreateDevice(int latency)
        {
            WaveCallbackInfo callbackInfo = waveOutSettingsPanel.UseWindowCallbacks ? WaveCallbackInfo.NewWindow() : WaveCallbackInfo.FunctionCallback();
            WaveOut outputDevice = new WaveOut(callbackInfo);
            outputDevice.DeviceNumber = waveOutSettingsPanel.SelectedDeviceNumber;
            outputDevice.DesiredLatency = latency;
            // TODO: configurable number of buffers
            return outputDevice;
        }

        public UserControl CreateSettingsPanel()
        {
            this.waveOutSettingsPanel = new WaveOutSettingsPanel();
            return waveOutSettingsPanel;
        }

        public string Name
        {
            get { return "waveOut"; }
        }

        public bool IsAvailable
        {
            get { return WaveOut.DeviceCount > 0; }
        }
    }
}
