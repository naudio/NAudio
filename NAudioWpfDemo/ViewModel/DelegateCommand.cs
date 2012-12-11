using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace NAudioWpfDemo.ViewModel
{
    class DelegateCommand : ICommand
    {
        private readonly Action action;

        public DelegateCommand(Action action)
        {
            this.action = action;
        }

        public void Execute(object parameter)
        {
            action();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged = delegate { };
    }
}
