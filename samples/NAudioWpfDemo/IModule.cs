using System;
using System.Linq;
using System.Windows.Controls;

namespace NAudioWpfDemo
{
    public interface IModule
    {
        string Name { get; }
        UserControl UserInterface { get; }
        void Deactivate();
    }
}
