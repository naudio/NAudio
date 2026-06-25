using NAudio.Wave;
using System.Windows.Forms;

namespace NAudioDemo.AudioPlaybackDemo;

internal class AsioOutPlugin : IOutputDevicePlugin
{
    private AsioOutSettingsPanel settingsPanel;

    public IWavePlayer CreateDevice(int latency)
    {
        return new AsioOut(settingsPanel.SelectedDeviceName);
    }

    public UserControl CreateSettingsPanel()
    {
        settingsPanel = new AsioOutSettingsPanel();
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
