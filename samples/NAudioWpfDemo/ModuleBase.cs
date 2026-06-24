using System;
using System.Windows.Controls;

namespace NAudioWpfDemo
{
    abstract class ModuleBase : IModule
    {
        private UserControl view;
        
        protected abstract UserControl CreateViewAndViewModel();
        
        public abstract string Name { get; }

        public UserControl UserInterface => view ?? (view = CreateViewAndViewModel());

        public void Deactivate()
        {
            if (view != null)
            {
                var d = view.DataContext as IDisposable;
                d?.Dispose();
                view = null;
            }
        }
    }
}
