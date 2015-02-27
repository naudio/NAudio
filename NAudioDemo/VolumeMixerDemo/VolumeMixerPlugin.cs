using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NAudioDemo.VolumeMixerDemo
{
    /// <summary>
    /// Volume mixer with functionality of Win Vista and higher Volume Mixer.
    /// </summary>
    /// </summary>
    [Export(typeof(INAudioDemoPlugin))]
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
