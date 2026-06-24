using System.Windows.Controls;

namespace NAudioWpfDemo.WasapiCaptureDemo
{
    class WasapiCaptureDemoPlugin : IModule
    {
        private WasapiCaptureViewModel viewModel;
        private WasapiCaptureDemoView view;

        public string Name => "WASAPI Capture";

        public UserControl UserInterface
        {
            get { if (view == null) CreateView(); return view; }
        }

        private void CreateView()
        {
            view = new WasapiCaptureDemoView();
            viewModel = new WasapiCaptureViewModel();
            view.DataContext = viewModel;
        }

        public void Deactivate()
        {
            viewModel.Dispose();
            view = null;
        }
    }
}
