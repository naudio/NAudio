using System;
using System.Linq;
using System.Windows;
using NAudio.MediaFoundation;
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
            // Start Media Foundation once for the whole app. MFStartup/MFShutdown are intended
            // to be paired at process scope — pairing them per panel risks deadlock if any MF
            // object outlives the panel.
            MediaFoundationApi.Startup();

            var mainWindow = new MainWindow();

            var modules = ReflectionHelper.CreateAllInstancesOf<IModule>();

            var vm = new MainWindowViewModel(modules);
            mainWindow.DataContext = vm;
            mainWindow.Closing += (s, args) => vm.SelectedModule.Deactivate();
            mainWindow.Show();
        }
    }
}
