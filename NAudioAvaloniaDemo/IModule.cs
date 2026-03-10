using Avalonia.Controls;
using System;
using System.Linq;

namespace NAudioAvaloniaDemo
{
    public interface IModule
    {
        string Name { get; }
        UserControl UserInterface { get; }
        void Deactivate();
    }
}
