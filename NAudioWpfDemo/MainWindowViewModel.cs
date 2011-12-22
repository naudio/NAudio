using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Controls;

namespace NAudioWpfDemo
{
    class MainWindowViewModel : INotifyPropertyChanged
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
                    RaisePropertyChanged("SelectedModule");
                    RaisePropertyChanged("UserInterface");
                }
            }
        }

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
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

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
