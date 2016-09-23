using System;
using System.Linq;
using System.Windows;
using NAudioWpfDemo.Utils;

namespace NAudioWpfDemo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var mainWindow = new MainWindow();

            var modules = ReflectionHelper.CreateAllInstancesOf<IModule>();

            var vm = new MainWindowViewModel(modules);
            mainWindow.DataContext = vm;
            mainWindow.Closing += (s, args) => vm.SelectedModule.Deactivate();
            mainWindow.Show();
        }
    }
}
