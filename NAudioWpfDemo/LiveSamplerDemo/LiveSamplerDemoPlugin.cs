using System.Windows.Controls;

namespace NAudioWpfDemo.LiveSamplerDemo
{
    class LiveSamplerDemoPlugin : ModuleBase
    {
        public override string Name => "Live MIDI Sampler";

        protected override UserControl CreateViewAndViewModel()
        {
            return new LiveSamplerDemoView { DataContext = new LiveSamplerDemoViewModel() };
        }
    }
}
