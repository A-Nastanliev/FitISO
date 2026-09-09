using Android.Content;
using Android.Content.Res;
using Android.Widget;
using AndroidX.Core.Content;
using SkiaSharp;

namespace FitISO.Maui.Platforms.Android
{
    public static class WidgetTheme
    {
        public const string PrefsName = "FitISO.WidgetTheme";
        public const string ThemeNameKey = "theme_name";
        const string DefaultThemeName = "Default";

        public const string ActionThemeChanged = "com.fitiso.maui.widget.THEME_CHANGED";

        public static ISharedPreferences? Prefs(Context context) =>
            context.GetSharedPreferences(PrefsName, FileCreationMode.Private);

        public static string ReadThemeName(ISharedPreferences? prefs) =>
            prefs?.GetString(ThemeNameKey, DefaultThemeName) ?? DefaultThemeName;

        public static int AccentColor(Context context, string themeName) =>
            ContextCompat.GetColor(context, AccentColorRes(themeName));

        public static int GridColor(Context context, string themeName) =>
            ContextCompat.GetColor(context, GridColorRes(themeName));

        public static int BackgroundColor(Context context, string themeName) =>
            ContextCompat.GetColor(context, BackgroundColorRes(themeName));

        public static int RestColor(Context context, string themeName) =>
            ContextCompat.GetColor(context, RestColorRes(themeName));

        public static int FutureColor(Context context, string themeName) =>
            ContextCompat.GetColor(context, FutureColorRes(themeName));

        public static int AccentColor(Context context, ISharedPreferences? prefs) =>
            AccentColor(context, ReadThemeName(prefs));

        public static int GridColor(Context context, ISharedPreferences? prefs) =>
            GridColor(context, ReadThemeName(prefs));

        public static int BackgroundColor(Context context, ISharedPreferences? prefs) =>
            BackgroundColor(context, ReadThemeName(prefs));

        public static int RestColor(Context context, ISharedPreferences? prefs) =>
            RestColor(context, ReadThemeName(prefs));

        public static int FutureColor(Context context, ISharedPreferences? prefs) =>
            FutureColor(context, ReadThemeName(prefs));


        static int AccentColorRes(string themeName) => themeName switch
        {
            "Slate" => Resource.Color.widget_accent_slate,
            "DarkRed" => Resource.Color.widget_accent_darkred,
            "Rust" => Resource.Color.widget_accent_rust,
            "Espresso" => Resource.Color.widget_accent_espresso,
            "Amber" => Resource.Color.widget_accent_amber,
            "Olive" => Resource.Color.widget_accent_olive,
            "Forest" => Resource.Color.widget_accent_forest,
            "DeepTeal" => Resource.Color.widget_accent_deepteal,
            "DarkBlue" => Resource.Color.widget_accent_darkblue,
            "Ink" => Resource.Color.widget_accent_ink,
            "Midnight" => Resource.Color.widget_accent_midnight,
            "Plum" => Resource.Color.widget_accent_plum,
            "Mauve" => Resource.Color.widget_accent_mauve,
            "Wine" => Resource.Color.widget_accent_wine,
            _ => Resource.Color.widget_accent_default,
        };

        static int GridColorRes(string themeName) => themeName switch
        {
            "Slate" => Resource.Color.widget_grid_slate,
            "DarkRed" => Resource.Color.widget_grid_darkred,
            "Rust" => Resource.Color.widget_grid_rust,
            "Espresso" => Resource.Color.widget_grid_espresso,
            "Amber" => Resource.Color.widget_grid_amber,
            "Olive" => Resource.Color.widget_grid_olive,
            "Forest" => Resource.Color.widget_grid_forest,
            "DeepTeal" => Resource.Color.widget_grid_deepteal,
            "DarkBlue" => Resource.Color.widget_grid_darkblue,
            "Ink" => Resource.Color.widget_grid_ink,
            "Midnight" => Resource.Color.widget_grid_midnight,
            "Plum" => Resource.Color.widget_grid_plum,
            "Mauve" => Resource.Color.widget_grid_mauve,
            "Wine" => Resource.Color.widget_grid_wine,
            _ => Resource.Color.widget_grid_default,
        };

        static int BackgroundColorRes(string themeName) => themeName switch
        {
            "Slate" => Resource.Color.widget_bg_slate,
            "DarkRed" => Resource.Color.widget_bg_darkred,
            "Rust" => Resource.Color.widget_bg_rust,
            "Espresso" => Resource.Color.widget_bg_espresso,
            "Amber" => Resource.Color.widget_bg_amber,
            "Olive" => Resource.Color.widget_bg_olive,
            "Forest" => Resource.Color.widget_bg_forest,
            "DeepTeal" => Resource.Color.widget_bg_deepteal,
            "DarkBlue" => Resource.Color.widget_bg_darkblue,
            "Ink" => Resource.Color.widget_bg_ink,
            "Midnight" => Resource.Color.widget_bg_midnight,
            "Plum" => Resource.Color.widget_bg_plum,
            "Mauve" => Resource.Color.widget_bg_mauve,
            "Wine" => Resource.Color.widget_bg_wine,
            _ => Resource.Color.widget_bg_default,
        };

        static int RestColorRes(string themeName) => themeName switch
        {
            "Slate" => Resource.Color.widget_rest_slate,
            "DarkRed" => Resource.Color.widget_rest_darkred,
            "Rust" => Resource.Color.widget_rest_rust,
            "Espresso" => Resource.Color.widget_rest_espresso,
            "Amber" => Resource.Color.widget_rest_amber,
            "Olive" => Resource.Color.widget_rest_olive,
            "Forest" => Resource.Color.widget_rest_forest,
            "DeepTeal" => Resource.Color.widget_rest_deepteal,
            "DarkBlue" => Resource.Color.widget_rest_darkblue,
            "Ink" => Resource.Color.widget_rest_ink,
            "Midnight" => Resource.Color.widget_rest_midnight,
            "Plum" => Resource.Color.widget_rest_plum,
            "Mauve" => Resource.Color.widget_rest_mauve,
            "Wine" => Resource.Color.widget_rest_wine,
            _ => Resource.Color.widget_rest_default,
        };

        static int FutureColorRes(string themeName) => themeName switch
        {
            "Slate" => Resource.Color.widget_future_slate,
            "DarkRed" => Resource.Color.widget_future_darkred,
            "Rust" => Resource.Color.widget_future_rust,
            "Espresso" => Resource.Color.widget_future_espresso,
            "Amber" => Resource.Color.widget_future_amber,
            "Olive" => Resource.Color.widget_future_olive,
            "Forest" => Resource.Color.widget_future_forest,
            "DeepTeal" => Resource.Color.widget_future_deepteal,
            "DarkBlue" => Resource.Color.widget_future_darkblue,
            "Ink" => Resource.Color.widget_future_ink,
            "Midnight" => Resource.Color.widget_future_midnight,
            "Plum" => Resource.Color.widget_future_plum,
            "Mauve" => Resource.Color.widget_future_mauve,
            "Wine" => Resource.Color.widget_future_wine,
            _ => Resource.Color.widget_future_default,
        };

        public static SKColor ToSkColor(int argb) => new SKColor(
            (byte)((argb >> 16) & 0xFF),
            (byte)((argb >> 8) & 0xFF),
            (byte)(argb & 0xFF),
            (byte)((argb >> 24) & 0xFF));

        public static void ApplyTint(this RemoteViews views, int viewId, int argb)
        {
            var color = new global::Android.Graphics.Color(argb);

            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                views.SetColorStateList(viewId, "setBackgroundTintList",
                    ColorStateList.ValueOf(color));
            }
            else
            {
                views.SetInt(viewId, "setBackgroundColor", argb);
            }
        }
    }
}