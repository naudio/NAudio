using System;
using System.Linq;
using System.Windows.Controls;

namespace NAudioWpfDemo.MediaFoundationEncode
{
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
