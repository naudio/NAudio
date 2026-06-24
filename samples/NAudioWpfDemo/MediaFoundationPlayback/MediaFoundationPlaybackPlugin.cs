using System.Windows.Controls;

namespace NAudioWpfDemo.MediaFoundationPlayback
{
    class MediaFoundationPlaybackPlugin : ModuleBase
    {
        protected override UserControl CreateViewAndViewModel()
        {
            return new MediaFoundationPlaybackView() { DataContext = new MediaFoundationPlaybackViewModel() };
        }

        public override string Name => "Media Foundation Playback";
    }
}
