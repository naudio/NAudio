using System.ComponentModel.Composition;
using System.Windows.Controls;
using NAudioWpfDemo.MediaFoundationEncode;

namespace NAudioWpfDemo.MediaFoundationResample
{
    [Export(typeof(IModule))]
    class MediaFoundationResamplePlugin : ModuleBase
    {
        protected override UserControl CreateViewAndViewModel()
        {
            return new MediaFoundationResampleView() {DataContext = new MediaFoundationResampleViewModel()};
        }

        public override string Name
        {
            get { return "Media Foundation Resample"; }
        }
    }
}
