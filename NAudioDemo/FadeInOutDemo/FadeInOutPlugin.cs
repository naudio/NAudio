using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;

namespace NAudioDemo.FadeInOutDemo
{
    [Export(typeof(INAudioDemoPlugin))]
    class FadeInOutPlugin : INAudioDemoPlugin
    {
        public string Name
        {
            get { return "Fade In Out"; }
        }

        public System.Windows.Forms.Control CreatePanel()
        {
            return new FadeInOutPanel();
        }
    }
}
