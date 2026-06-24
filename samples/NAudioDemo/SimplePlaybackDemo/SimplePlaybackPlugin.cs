using System;
using System.Linq;
using System.Windows.Forms;

namespace NAudioDemo.SimplePlaybackDemo
{
    class SimplePlaybackPlugin : INAudioDemoPlugin
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
}
