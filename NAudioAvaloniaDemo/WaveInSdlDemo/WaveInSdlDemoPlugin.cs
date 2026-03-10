using Avalonia.Controls;

namespace NAudioAvaloniaDemo.WaveInSdlDemo
{
    class WaveInSdlDemoPlugin : IModule
    {
        private WaveInSdlViewModel viewModel;
        private WaveInSdlDemoView view;

        public string Name => "WaveInSdl";

        public UserControl UserInterface
        {
            get { if (view == null) CreateView(); return view; }
        }

        private void CreateView()
        {
            view = new WaveInSdlDemoView();
            viewModel = new WaveInSdlViewModel();
            view.DataContext = viewModel;
        }

        public void Deactivate()
        {
            viewModel.Dispose();
            view = null;
        }
    }
}
