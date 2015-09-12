using System;
using System.Linq;
using System.Windows.Forms;

namespace NAudioDemo.AudioPlaybackDemo
{
    public class AudioPlaybackPanelPlugin : INAudioDemoPlugin
    {
        public string Name
        {
            get { return "Audio File Playback"; }
        }

        public Control CreatePanel()
        {
            return new AudioPlaybackPanel();
        }
    }
}
