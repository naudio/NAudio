using System;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace NAudioDemo.AudioPlaybackDemo
{
    /// <summary>
    /// Plugin exposing the legacy <see cref="WasapiOut"/> API. Listed alongside
    /// <see cref="WasapiPlayerPlugin"/> so users can A/B the two code paths.
    /// </summary>
    class WasapiOutPlugin : IOutputDevicePlugin
    {
        WasapiOutSettingsPanel settingsPanel;

        public IWavePlayer CreateDevice(int latency)
        {
#pragma warning disable CS0618 // legacy WasapiOut is intentionally exercised here
            return new WasapiOut(
                settingsPanel.SelectedDevice,
                settingsPanel.ShareMode,
                settingsPanel.UseEventCallback,
                latency);
#pragma warning restore CS0618
        }

        public UserControl CreateSettingsPanel()
        {
            settingsPanel = new WasapiOutSettingsPanel();
            return settingsPanel;
        }

        public string Name => "WasapiOut (legacy)";

        public bool IsAvailable => Environment.OSVersion.Version.Major >= 6;

        public int Priority => 2;
    }
}
