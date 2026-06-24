using System.Windows.Forms;

namespace NAudioDemo.SimplePlaybackDemo;

internal class SimplePlaybackPlugin : INAudioDemoPlugin
{
    public string Name
    {
        get { return "Simple Playback"; }
    }

    public Control CreatePanel()
    {
        return new SimplePlaybackPanel();
    }
}
