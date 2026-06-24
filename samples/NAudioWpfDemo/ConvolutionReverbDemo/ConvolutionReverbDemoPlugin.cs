using System.Windows.Controls;

namespace NAudioWpfDemo.ConvolutionReverbDemo
{
    class ConvolutionReverbDemoPlugin : IModule
    {
        private ConvolutionReverbDemoView view;
        private ConvolutionReverbDemoViewModel viewModel;

        public string Name => "Convolution Reverb";

        public UserControl UserInterface
        {
            get { if (view == null) CreateView(); return view; }
        }

        private void CreateView()
        {
            view = new ConvolutionReverbDemoView();
            viewModel = new ConvolutionReverbDemoViewModel();
            view.DataContext = viewModel;
        }

        public void Deactivate()
        {
            view = null;
            viewModel = null;
        }
    }
}
