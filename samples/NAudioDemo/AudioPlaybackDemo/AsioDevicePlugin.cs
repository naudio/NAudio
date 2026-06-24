using System.Windows.Forms;
using NAudio.Wave;

namespace NAudioDemo.AudioPlaybackDemo
{
    /// <summary>
    /// Plugin exposing the new NAudio 3 <see cref="AsioDevice"/> API through the demo's <see cref="IOutputDevicePlugin"/>
    /// contract. Listed alongside the legacy <c>AsioOut</c> plugin so users can A/B the two code paths.
    /// </summary>
    class AsioDevicePlugin : IOutputDevicePlugin
    {
        private AsioDeviceSettingsPanel settingsPanel;

        public IWavePlayer CreateDevice(int latency)
        {
            return new AsioDeviceAdapter(settingsPanel.SelectedDeviceName, settingsPanel.OutputChannelOffset);
        }

        public UserControl CreateSettingsPanel()
        {
            settingsPanel = new AsioDeviceSettingsPanel();
            return settingsPanel;
        }

        public string Name => "AsioDevice (NAudio 3)";

        public bool IsAvailable => AsioDevice.GetDriverNames().Length > 0;

        public int Priority => 5;
    }
}
