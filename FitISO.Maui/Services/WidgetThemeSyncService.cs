using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
#if ANDROID
using FitISO.Maui.Platforms.Android;
#endif

namespace FitISO.Maui.Services
{
    public class WidgetThemeSyncService : IRecipient<AccentThemeChangedMessage>
    {
        public WidgetThemeSyncService()
        {
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        public void Receive(AccentThemeChangedMessage message)
        {
            WriteThemeName(message.Value);
            BroadcastThemeChanged();
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