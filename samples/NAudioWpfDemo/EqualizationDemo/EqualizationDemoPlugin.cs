using System.Windows.Controls;

namespace NAudioWpfDemo.EqualizationDemo
{
    class EqualizationPlaybackDemoPlugin : IModule
    {
        private EqualizationDemoView view;
        private EqualizationDemoViewModel viewModel;

        public string Name => "Graphic EQ";

        public UserControl UserInterface
        {
            get { if (view == null) CreateView(); return view; }
        }

        private void CreateView()
        {
            view = new EqualizationDemoView();
            viewModel = new EqualizationDemoViewModel();
            view.DataContext = viewModel;
        }

        public void Deactivate()
        {
            viewModel.Dispose();
            view = null;
        }
    }
}
