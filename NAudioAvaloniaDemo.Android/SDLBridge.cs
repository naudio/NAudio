using Android.App;
using Microsoft.Maui.ApplicationModel;
using NAudio.Sdl2.Interop;
using System.Threading.Tasks;
using SDLBridgeProvider = Org.Libsdl.App.SDL;

namespace NAudioAvaloniaDemo.Android
{
    public static class SDLBridge
    {
        public static void Load(Activity activity)
        {
            SDLBridgeProvider.LoadLibrary("SDL2");
            SDLBridgeProvider.SetupJNI();
            SDLBridgeProvider.Initialize();
            SDLBridgeProvider.Context = activity;
        }

        public static void Unload(Activity activity)
        {
            SDL.SDL_Quit();
            if (SDLBridgeProvider.Context == activity)
            {
                SDLBridgeProvider.Context = null;
            }
        }

        public static async Task RequestPermissions()
        {
            await Permissions.RequestAsync<Permissions.Microphone>();
            await Permissions.RequestAsync<Permissions.StorageRead>();
            await Permissions.RequestAsync<Permissions.StorageWrite>();
        }
    }
}
