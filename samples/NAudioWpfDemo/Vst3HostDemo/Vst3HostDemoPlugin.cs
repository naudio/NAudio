using System.Windows.Controls;

namespace NAudioWpfDemo.Vst3HostDemo;

internal class Vst3HostDemoPlugin : IModule
{
    private Vst3HostDemoView view;
    private Vst3HostDemoViewModel viewModel;

    public string Name => "VST3 Effect Host";

    public UserControl UserInterface
    {
        get
        {
            if (view == null)
            {
                view = new Vst3HostDemoView();
                viewModel = new Vst3HostDemoViewModel();
                view.DataContext = viewModel;
            }
            return view;
        }
    }

    public void Deactivate()
    {
        viewModel?.Dispose();
        view = null;
        viewModel = null;
    }
}
