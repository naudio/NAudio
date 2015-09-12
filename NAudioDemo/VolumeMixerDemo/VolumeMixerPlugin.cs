using System;
using System.Linq;
using System.Windows.Forms;

namespace NAudioDemo.VolumeMixerDemo
{
    /// <summary>
    /// Volume mixer with functionality of Win Vista and higher Volume Mixer.
    /// </summary>
    class VolumeMixerPlugin : INAudioDemoPlugin
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
}
