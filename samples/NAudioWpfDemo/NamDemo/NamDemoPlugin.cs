using System.Windows.Controls;

namespace NAudioWpfDemo.NamDemo;

internal class NamDemoPlugin : IModule
{
    private NamDemoView view;
    private NamDemoViewModel viewModel;

    public string Name => "Neural Amp Modeler";

    public UserControl UserInterface
    {
        get { if (view == null) CreateView(); return view; }
    }

    private void CreateView()
    {
        view = new NamDemoView();
        viewModel = new NamDemoViewModel();
        view.DataContext = viewModel;
    }

    public void Deactivate()
    {
        viewModel?.Dispose();
        viewModel = null;
        view = null;
    }
}
