using System.Windows.Controls;

namespace NAudioWpfDemo.MediaFoundationEncode;

internal class MediaFoundationEncodePlugin : ModuleBase
{
    protected override UserControl CreateViewAndViewModel()
    {
        return new MediaFoundationEncodeView() { DataContext = new MediaFoundationEncodeViewModel() };
    }

    public override string Name => "Media Foundation Encode";
}
