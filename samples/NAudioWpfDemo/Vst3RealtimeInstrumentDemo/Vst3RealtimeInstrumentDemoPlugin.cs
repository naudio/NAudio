using System.Windows.Controls;

namespace NAudioWpfDemo.Vst3RealtimeInstrumentDemo;

internal class Vst3RealtimeInstrumentDemoPlugin : IModule
{
    private Vst3RealtimeInstrumentView view;
    private Vst3RealtimeInstrumentViewModel viewModel;

    public string Name => "VST3 Realtime Instrument";

    public UserControl UserInterface
    {
        get { if (view == null) CreateView(); return view; }
    }

    private void CreateView()
    {
        view = new Vst3RealtimeInstrumentView();
        viewModel = new Vst3RealtimeInstrumentViewModel();
        view.DataContext = viewModel;
    }

    public void Deactivate()
    {
        viewModel?.Dispose();
        view = null;
        viewModel = null;
    }
}
