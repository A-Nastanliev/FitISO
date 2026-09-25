using Android.Content;
using Android.Database;
using Android.OS;
using Android.Provider;
using FitISO.Maui.Services;

namespace FitISO.Maui.Platforms.Android
{
    public class ThemeOverlayObserver : ContentObserver
    {
        readonly Context _context;

        ThemeOverlayObserver(Context context, Handler handler) : base(handler)
        {
            _context = context;
        }

        public static void Register(Context context)
        {
            var handler = new Handler(Looper.MainLooper!);
            var observer = new ThemeOverlayObserver(context, handler);

            var uri = Settings.Secure.GetUriFor("theme_customization_overlay_packages");
            context.ContentResolver?.RegisterContentObserver(uri, false, observer);
        }

        public override void OnChange(bool selfChange)
        {
            base.OnChange(selfChange);
            AccentThemeService.ScheduleDynamicRefresh(_context);
        }
    }
}