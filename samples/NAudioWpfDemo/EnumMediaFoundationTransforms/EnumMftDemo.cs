using System.Windows.Controls;

namespace NAudioWpfDemo.EnumMediaFoundationTransforms
{
    class EnumMftModule : ModuleBase
    {
        protected override UserControl CreateViewAndViewModel()
        {
            return new EnumMftView() { DataContext = new EnumMftViewModel() };
        }

        public override string Name => "Enumerate MFTs";
    }
}
