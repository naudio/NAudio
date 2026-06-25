using System.Windows.Controls;

namespace NAudioWpfDemo.Vst3RealtimeEffectDemo;

class Vst3RealtimeEffectDemoPlugin : IModule
{
    private Vst3RealtimeEffectView view;
    private Vst3RealtimeEffectViewModel viewModel;

    public string Name => "VST3 Realtime Effects";

    public UserControl UserInterface
    {
        get { if (view == null) CreateView(); return view; }
    }

    private void CreateView()
    {
        view = new Vst3RealtimeEffectView();
        viewModel = new Vst3RealtimeEffectViewModel();
        view.DataContext = viewModel;
    }

    public void Deactivate()
    {
        viewModel?.Dispose();
        view = null;
        viewModel = null;
    }
}
