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
            var fallback = new Default();

            var gray100 = (Color)fallback["Gray100"];
            var gray950 = (Color)fallback["Gray950"];

#if ANDROID
            var context = global::Android.App.Application.Context;
            var palette = DynamicColorReader.ReadAccentTonalPalette(context);

            if (palette is not null && HasUsableContrast(palette))
            {
                this["Gray100"] = ToColor(palette[0]);
                this["Gray200"] = ToColor(palette[1]);
                this["Gray300"] = ToColor(palette[2]);
                this["Gray400"] = ToColor(palette[3]);
                this["Gray500"] = ToColor(palette[5]);
                this["Gray600"] = ToColor(palette[7]);
                this["Gray700"] = ToColor(palette[8]);
                this["Gray800"] = ToColor(palette[9]);
                this["Gray900"] = ToColor(palette[9]);
                this["Gray950"] = ToColor(palette[10]);
            }
            else
#endif
            {
                this["Gray100"] = gray100;
                this["Gray200"] = (Color)fallback["Gray200"];
                this["Gray300"] = (Color)fallback["Gray300"];
                this["Gray400"] = (Color)fallback["Gray400"];
                this["Gray500"] = (Color)fallback["Gray500"];
                this["Gray600"] = (Color)fallback["Gray600"];
                this["Gray700"] = (Color)fallback["Gray700"];
                this["Gray800"] = (Color)fallback["Gray800"];
                this["Gray900"] = (Color)fallback["Gray900"];
                this["Gray950"] = gray950;
            }

            this["ChartAccentColor"] = (Color)fallback["ChartAccentColor"];
            this["DeleteColor"] = (Color)fallback["DeleteColor"];
        }

        static Color ToColor(int argb) => Color.FromRgba(
            (argb >> 16) & 0xFF,
            (argb >> 8) & 0xFF,
            argb & 0xFF,
            (argb >> 24) & 0xFF);

        static bool HasUsableContrast(int[] palette)
        {
            var lightest = RelativeLuminance(palette[0]);
            var darkest = RelativeLuminance(palette[^1]);
            return (lightest - darkest) > 0.35;
        }

        static double RelativeLuminance(int argb)
        {
            double Channel(int c)
            {
                var v = c / 255.0;
                return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
            }

            var r = Channel((argb >> 16) & 0xFF);
            var g = Channel((argb >> 8) & 0xFF);
            var b = Channel(argb & 0xFF);
            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }
    }
}