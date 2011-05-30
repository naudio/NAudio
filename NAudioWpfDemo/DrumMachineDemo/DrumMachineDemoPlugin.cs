using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;

namespace NAudioWpfDemo.DrumMachineDemo
{
    // HIDE FROM VIEW - STILL A WORK IN PROGRESS [Export(typeof(IModule))]
    class DrumMachineDemoPlugin : IModule
    {
        private DrumMachineDemoView view;
        private DrumMachineDemoViewModel viewModel;
        public string Name
        {
            get { return "Drum Machine"; }
        }

        public System.Windows.Controls.UserControl UserInterface
        {
            get 
            {
                if (view == null)
                {
                    view = new DrumMachineDemoView();
                    viewModel = new DrumMachineDemoViewModel();
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
