using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;

namespace NAudioWpfDemo.EqualizationDemo
{
    [Export(typeof(IModule))]
    class EqualizationPlaybackDemoPlugin : IModule
    {
        private EqualizationDemoView view;
        private EqualizationDemoViewModel viewModel;

        [ImportingConstructor]
        public EqualizationPlaybackDemoPlugin()
        {
        }

        public string Name
        {
            get { return "Graphic EQ"; }
        }

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
