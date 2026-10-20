using System.Windows.Forms;

namespace NAudioDemo.VolumeMixerDemo;

/// <summary>
/// Volume mixer with the functionality of the Windows Volume Mixer.
/// </summary>
internal class VolumeMixerPlugin : INAudioDemoPlugin
{
    public string Name
    {
        get { return "Volume Mixer"; }
    }

    public Control CreatePanel()
    {
        return new VolumeMixerPanel();
    }
}
