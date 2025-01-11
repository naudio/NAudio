using System;
using System.Linq;
using System.Windows.Input;

namespace NAudioAvaloniaDemo.ViewModel
{
    class DelegateCommand : ICommand
    {
        private readonly Action action;
        private bool isEnabled;

        public DelegateCommand(Action action)
        {
            this.action = action;
            isEnabled = true;
        }

        public void Execute(object parameter)
        {
            action();
        }

        public bool CanExecute(object parameter)
        {
            return isEnabled;
        }

        public bool IsEnabled
        {
            get
            {
                return isEnabled;
            }
            set
            {
                if (isEnabled != value)
                {
                    isEnabled = value;
                    OnCanExecuteChanged();
                }
            }
        }

        public event EventHandler CanExecuteChanged;

        protected virtual void OnCanExecuteChanged()
        {
            EventHandler handler = CanExecuteChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
