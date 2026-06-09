using System.Windows.Controls;

namespace NAudioWpfDemo.SampleEditorDemo
{
    class SampleEditorPlugin : ModuleBase
    {
        public override string Name => "Single-Sample Editor";

        protected override UserControl CreateViewAndViewModel()
        {
            return new SampleEditorView { DataContext = new SampleEditorViewModel() };
        }
    }
}
