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
            // Surface unhandled exceptions to the user instead of silently tearing the app down,
            // so a buggy demo panel produces a visible error rather than a vanish-on-click crash.
            DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show(args.Exception.ToString(), "Unhandled exception", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

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
