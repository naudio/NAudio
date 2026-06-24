using System;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace NAudioDemo.AudioPlaybackDemo
{
    /// <summary>
    /// Plugin exposing the new NAudio 3 <see cref="WasapiPlayer"/> API (built via
    /// <see cref="WasapiPlayerBuilder"/>). Listed alongside the legacy
    /// <see cref="WasapiOutPlugin"/> so users can A/B the two code paths.
    /// </summary>
    class WasapiPlayerPlugin : IOutputDevicePlugin
    {
        WasapiOutSettingsPanel settingsPanel;

        public IWavePlayer CreateDevice(int latency)
        {
            var builder = new WasapiPlayerBuilder()
                .WithDevice(settingsPanel.SelectedDevice)
                .WithLatency(latency);

            if (settingsPanel.ShareMode == AudioClientShareMode.Exclusive)
                builder.WithExclusiveMode();
            else
                builder.WithSharedMode();

            if (settingsPanel.UseEventCallback)
                builder.WithEventSync();

            return builder.Build();
        }

        public UserControl CreateSettingsPanel()
        {
            settingsPanel = new WasapiOutSettingsPanel();
            return settingsPanel;
        }

        public string Name => "WasapiPlayer (NAudio 3)";

        public bool IsAvailable => Environment.OSVersion.Version.Major >= 6;

        public int Priority => 1;
    }
}
