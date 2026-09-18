#if ANDROID
using FitISO.Maui.Platforms.Android;
#endif

namespace FitISO.Maui.Services
{
    public class AccentThemeService
    {
        const string AccentThemeKey = "accent_theme";
        const string DefaultThemeName = "Default";

        public string AccentThemeName
        {
            get => Preferences.Default.Get(AccentThemeKey, DefaultThemeName);
            set
            {
                Preferences.Default.Set(AccentThemeKey, value);
                WriteThemeName(value);
                BroadcastThemeChanged();
            }
        }

#if ANDROID
        static void WriteThemeName(string themeName)
        {
            var context = global::Android.App.Application.Context;
            var prefs = WidgetTheme.Prefs(context);
            prefs?.Edit()?.PutString(WidgetTheme.ThemeNameKey, themeName)?.Commit();
        }

        static void BroadcastThemeChanged()
        {
            var context = global::Android.App.Application.Context;
            var intent = new global::Android.Content.Intent(WidgetTheme.ActionThemeChanged);
            intent.SetPackage(context.PackageName);
            context.SendBroadcast(intent);
        }
#else
        static void WriteThemeName(string themeName) { }
        static void BroadcastThemeChanged() { }
#endif
    }
}