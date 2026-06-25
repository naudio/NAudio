using System.Windows.Controls;

namespace NAudioWpfDemo.RealtimeEffectsDemo;

internal class RealtimeEffectsDemoPlugin : IModule
{
    private RealtimeEffectsView view;
    private RealtimeEffectsViewModel viewModel;

    public string Name => "Realtime Effects";

    public UserControl UserInterface
    {
        get { if (view == null) CreateView(); return view; }
    }

    private void CreateView()
    {
        view = new RealtimeEffectsView();
        viewModel = new RealtimeEffectsViewModel();
        view.DataContext = viewModel;
    }

    public void Deactivate()
    {
        viewModel?.Dispose();
        view = null;
    }
}
