using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using Microsoft.Maui.ApplicationModel;
using System.Threading.Tasks;

namespace NAudioAvaloniaDemo.Android
{
    [Activity(
        Label = "NAudioAvaloniaDemo",
        Theme = "@style/MyTheme.NoActionBar",
        Icon = "@drawable/icon",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    public class MainActivity : AvaloniaMainActivity<App>
    {
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            SDLBridge.Load(this);
            await RequestPermissions();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            SDLBridge.Unload(this);
        }

        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            return base.CustomizeAppBuilder(builder)
                .WithInterFont();
        }

        private static async Task RequestPermissions()
        {
            await Permissions.RequestAsync<Permissions.Microphone>();
            await Permissions.RequestAsync<Permissions.StorageRead>();
            await Permissions.RequestAsync<Permissions.StorageWrite>();
        }
    }
}
