using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;

namespace NAudioWpfDemo.ViewModel
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> execute;
        private readonly Predicate<object> canExecute;

        public RelayCommand(Action<object> execute)
            : this(execute, null)
        {
        }

        public RelayCommand(Action execute, Func<bool> canExecute)
            : this((s) => execute(), (s) => canExecute())
        {
            
        }

        public RelayCommand(Action execute)
            : this((s) => execute(), null)
        {

        }

        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            return canExecute == null || canExecute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            execute(parameter);
        }

    }
}
