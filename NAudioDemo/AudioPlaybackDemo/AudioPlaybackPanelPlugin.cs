using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.Windows.Forms;

namespace NAudioDemo.AudioPlaybackDemo
{
    [Export(typeof(INAudioDemoPlugin))]
    public class AudioPlaybackPanelPlugin : INAudioDemoPlugin
    {
        public string Name
        {
            get { return "Audio File Playback"; }
        }

        // using ExportFactory<T> rather than Lazy<T> allowing us to create 
        // a new one each time
        // had to download a special MEF extension to allow this in .NET 3.5
        [Import]
        public ExportFactory<AudioPlaybackPanel> PanelFactory { get; set; }

        public Control CreatePanel()
        {
            return PanelFactory.CreateExport().Value; //new AudioPlaybackPanel();
        }
    }
}
