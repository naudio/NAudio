using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace NAudioWpfDemo.MediaFoundationPlayback
{
    [Export(typeof(IModule))]
    class MediaFoundationPlaybackPlugin : ModuleBase
    {
        protected override UserControl CreateViewAndViewModel()
        {
            return new MediaFoundationPlaybackView() { DataContext = new MediaFoundationPlaybackViewModel() };
        }

        public override string Name
        {
            get { return "Media Foundation Playback"; }
        }
    }
}
