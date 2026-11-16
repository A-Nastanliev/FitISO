#if ANDROID
using FitISO.Maui.Platforms.Android;
#endif

namespace FitISO.Maui.Resources.Styles.AccentThemes
{
    public partial class DynamicAccentTheme : ResourceDictionary
    {
        public static bool IsAvailable =>
#if ANDROID
            DynamicColorReader.IsAvailable;
#else
            false;
#endif

        public DynamicAccentTheme()
        {
            foreach (var entry in new Default())
                this[entry.Key] = entry.Value;

#if ANDROID
            var palette = MaterialDarkPalette.Read(global::Android.App.Application.Context);
            if (palette is null)
                return;

            this["Gray100"] = ToColor(palette.Gray100);
            this["Gray200"] = ToColor(palette.Gray200);
            this["Gray300"] = ToColor(palette.Gray300);
            this["Gray400"] = ToColor(palette.Gray400);
            this["Gray500"] = ToColor(palette.Gray500);
            this["Gray600"] = ToColor(palette.Gray600);
            this["Gray700"] = ToColor(palette.Gray700);
            this["Gray800"] = ToColor(palette.Gray800);
            this["Gray900"] = ToColor(palette.Gray900);
            this["Gray950"] = ToColor(palette.Gray950);

            this["ChartAccentColor"] = ToColor(palette.Accent);
            this["SwatchColor"] = ToColor(palette.Accent);
#endif
        }

        static Color ToColor(int argb) => Color.FromRgba(
            (argb >> 16) & 0xFF,
            (argb >> 8) & 0xFF,
            argb & 0xFF,
            (argb >> 24) & 0xFF);
    }
}