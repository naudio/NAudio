using System.Windows.Controls;

namespace NAudioWpfDemo.Vst3MidiFileDemo;

class Vst3MidiFileDemoPlugin : ModuleBase
{
    public override string Name => "VST3 MIDI File Player";

    protected override UserControl CreateViewAndViewModel()
    {
        return new Vst3MidiFileDemoView { DataContext = new Vst3MidiFileDemoViewModel() };
    }
}
