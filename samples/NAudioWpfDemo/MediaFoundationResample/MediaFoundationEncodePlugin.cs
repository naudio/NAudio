using System.Windows.Controls;

namespace NAudioWpfDemo.MediaFoundationResample;

internal class MediaFoundationResamplePlugin : ModuleBase
{
    protected override UserControl CreateViewAndViewModel()
    {
        return new MediaFoundationResampleView() { DataContext = new MediaFoundationResampleViewModel() };
    }

    public override string Name => "Media Foundation Resample";
}
