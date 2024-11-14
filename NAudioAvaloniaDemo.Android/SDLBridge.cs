using Android.App;
using Org.Libsdl.App;

namespace NAudioAvaloniaDemo.Android
{
    public static class SDLBridge
    {
        public static void Load(Activity activity)
        {
            SDL.LoadLibrary("SDL2");
            SDL.SetupJNI();
            SDL.Initialize();
            SDL.Context = activity;
        }

        public static void Unload(Activity activity)
        {
            if (SDL.Context == activity)
            {
                SDL.Context = null;
            }
        }
    }
}
