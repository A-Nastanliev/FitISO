using Android.App;
using Android.Content;
using Android.OS;
using FitISO.Maui.Services;

namespace FitISO.Maui.Platforms.Android
{
    public class WallpaperColorObserver : Java.Lang.Object, WallpaperManager.IOnColorsChangedListener
    {
        public static void Register(Context context)
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(27))
                return;

            var wallpaperManager = WallpaperManager.GetInstance(context);
            wallpaperManager?.AddOnColorsChangedListener(new WallpaperColorObserver(), new Handler(Looper.MainLooper!));
        }

        public void OnColorsChanged(WallpaperColors? colors, int which)
        {
            if (!((WallpaperManagerFlags)which).HasFlag(WallpaperManagerFlags.System))
                return;

            AccentThemeService.ScheduleDynamicRefresh(global::Android.App.Application.Context);
        }
    }
}