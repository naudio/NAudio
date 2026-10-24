using System.Windows.Controls;

namespace NAudioWpfDemo.ConvolutionReverbDemo;

internal class ConvolutionReverbDemoPlugin : ModuleBase
{
    public override string Name => "Convolution Reverb";

    protected override UserControl CreateViewAndViewModel()
    {
        return new ConvolutionReverbDemoView { DataContext = new ConvolutionReverbDemoViewModel() };
    }
}
