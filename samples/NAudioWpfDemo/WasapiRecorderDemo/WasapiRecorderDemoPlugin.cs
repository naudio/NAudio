using System.Windows.Controls;

namespace NAudioWpfDemo.WasapiRecorderDemo;

internal class WasapiRecorderDemoPlugin : IModule
{
    private WasapiRecorderViewModel viewModel;
    private WasapiRecorderDemoView view;

    public string Name => "WASAPI Recorder";

    public UserControl UserInterface
    {
        get { if (view == null) CreateView(); return view; }
    }

    private void CreateView()
    {
        view = new WasapiRecorderDemoView();
        viewModel = new WasapiRecorderViewModel();
        view.DataContext = viewModel;
    }

    public void Deactivate()
    {
        viewModel.Dispose();
        view = null;
    }
}
