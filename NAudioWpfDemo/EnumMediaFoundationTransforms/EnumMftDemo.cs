using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using NAudioWpfDemo.DrumMachineDemo;

namespace NAudioWpfDemo.EnumMediaFoundationTransforms
{
    [Export(typeof(IModule))]
    class EnumMftModule : IModule
    {
        private EnumMftView view;
        private EnumMftViewModel viewModel;
        public string Name
        {
            get { return "Enumerate MFTs"; }
        }

        public UserControl UserInterface
        {
            get
            {
                if (view == null)
                {
                    view = new EnumMftView();
                    viewModel = new EnumMftViewModel();
                    view.DataContext = viewModel;
                }
                return view;
            }
        }

        public void Deactivate()
        {
            if (view != null)
            {
                viewModel.Dispose();
                view = null;
                viewModel = null;
            }
        }
    }
}
