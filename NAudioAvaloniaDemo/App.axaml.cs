using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using NAudioAvaloniaDemo.Utils;
using NAudioAvaloniaDemo.Views;

namespace NAudioAvaloniaDemo
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);

            var modules = ReflectionHelper.CreateAllInstancesOf<IModule>();
            var vm = new MainWindowViewModel(modules);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = new MainWindow();
                mainWindow.DataContext = vm;
                mainWindow.Closing += (s, args) => vm.SelectedModule.Deactivate();
                mainWindow.Show();

                desktop.MainWindow = mainWindow;
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                var mainView = new MainView();
                mainView.DataContext = vm;
                mainView.Unloaded += (s, args) => vm.SelectedModule.Deactivate();
                singleViewPlatform.MainView = mainView;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}