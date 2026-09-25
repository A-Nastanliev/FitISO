#if ANDROID
using Android.OS;
using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
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

            if (themeName == WidgetTheme.DynamicThemeName)
            {
                var colors = DynamicWidgetColors.Compute(context);
                if (colors is not null)
                    WidgetTheme.WriteDynamicColors(prefs, colors);
            }
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

#if ANDROID
        static Handler? pendingRefresh;

        public static void ScheduleDynamicRefresh(global::Android.Content.Context context)
        {
            pendingRefresh?.RemoveCallbacksAndMessages(null);
            pendingRefresh = new Handler(Looper.MainLooper!);
            pendingRefresh.PostDelayed(() => RefreshDynamicColorsFromSystem(context), 300);
        }

        public static void RefreshDynamicColorsFromSystem(global::Android.Content.Context context)
        {
            var prefs = WidgetTheme.Prefs(context);
            if (WidgetTheme.ReadThemeName(prefs) != WidgetTheme.DynamicThemeName)
                return;

            var colors = DynamicWidgetColors.Compute(context);
            if (colors is null)
                return;

            WidgetTheme.WriteDynamicColors(prefs, colors);

            var intent = new global::Android.Content.Intent(WidgetTheme.ActionThemeChanged);
            intent.SetPackage(context.PackageName);
            context.SendBroadcast(intent);

            WeakReferenceMessenger.Default.Send(new DynamicAccentColorsChangedMessage());
        }
#endif
    }
}