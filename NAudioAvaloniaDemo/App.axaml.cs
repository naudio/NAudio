using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using NAudio.Sdl2.Interop;
using NAudioAvaloniaDemo.Utils;
using NAudioAvaloniaDemo.Views;

namespace NAudioAvaloniaDemo
{
    public partial class App : Application
    {
        public static Window MainWindow { get; private set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = new MainWindow();

                var modules = ReflectionHelper.CreateAllInstancesOf<IModule>();

                var vm = new MainWindowViewModel(modules);
                mainWindow.DataContext = vm;
                mainWindow.Closing += (s, args) => vm.SelectedModule.Deactivate();
                mainWindow.Show();

                desktop.ShutdownRequested += DesktopOnShutdownRequested;
                desktop.MainWindow = mainWindow;
                MainWindow = mainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void DesktopOnShutdownRequested(object sender, ShutdownRequestedEventArgs e)
        {
            // Clean up all initialized subsystems
            // It is safe to call this function even in the case of errors in initialization
            SDL.SDL_Quit();
        }
    }
}