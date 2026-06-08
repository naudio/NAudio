using System.Windows.Controls;

namespace NAudioWpfDemo.SamplerDemo
{
    class SamplerDemoPlugin : ModuleBase
    {
        public override string Name => "SoundFont / MIDI Player";

        protected override UserControl CreateViewAndViewModel()
        {
            return new SamplerDemoView { DataContext = new SamplerDemoViewModel() };
        }
    }
}
