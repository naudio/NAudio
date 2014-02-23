using System;
using System.Windows.Input;

namespace NAudioWin8Demo
{
    internal class DelegateCommand : ICommand
    {
        private readonly Action action;
        private bool enabled;

        public DelegateCommand(Action action)
        {
            this.action = action;
            this.enabled = true;
        }

        public bool IsEnabled
        {
            get { return enabled; }
            set
            {
                if (enabled != value)
                {
                    enabled = value;
                    OnCanExecuteChanged();
                }
            }
        }

        public bool CanExecute(object parameter)
        {
            return enabled;
        }

        public void Execute(object parameter)
        {
            action();
        }

        public event EventHandler CanExecuteChanged;

        protected virtual void OnCanExecuteChanged()
        {
            EventHandler handler = CanExecuteChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}