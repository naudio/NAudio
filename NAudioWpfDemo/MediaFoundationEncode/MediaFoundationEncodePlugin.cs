using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace NAudioWpfDemo.MediaFoundationEncode
{
    [Export(typeof(IModule))]
    class MediaFoundationEncodePlugin : ModuleBase
    {
        protected override UserControl CreateViewAndViewModel()
        {
            return new MediaFoundationEncodeView() {DataContext = new MediaFoundationEncodeViewModel()};
        }

        public override string Name
        {
            get { return "Media Foundation Encode"; }
        }
    }
}
