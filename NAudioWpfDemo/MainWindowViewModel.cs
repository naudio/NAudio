using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Controls;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo
{
    class MainWindowViewModel : ViewModelBase
    {
        private IModule selectedModule;

        public MainWindowViewModel(IEnumerable<IModule> modules)
        {
            this.Modules = modules.OrderBy(m => m.Name).ToList();
            if (this.Modules.Count > 0)
            {
                this.SelectedModule = this.Modules[0];
            }
        }

        public List<IModule> Modules { get; private set; }

        public IModule SelectedModule
        {
            get
            {
                return selectedModule;
            }
            set
            {
                if (value != selectedModule)
                {
                    if (selectedModule != null)
                    {
                        selectedModule.Deactivate();
                    }
                    selectedModule = value;
                    OnPropertyChanged("SelectedModule");
                    OnPropertyChanged("UserInterface");
                }
            }
        }

        public UserControl UserInterface
        {
            get
            {
                if (SelectedModule == null) return null;
                return SelectedModule.UserInterface;
            }
        }
    }
}
