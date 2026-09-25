using Android.Content;

namespace FitISO.Maui.Platforms.Android
{
    public static class DynamicWidgetColors
    {
        public record Result(int Accent, int Grid, int Background, int Rest, int Future);

        public static Result? Compute(Context context)
        {
            var palette = DynamicColorReader.ReadAccentTonalPalette(context);
            if (palette is null)
                return null;

            var background = palette[10]; 
            var accent = palette[6];    
            var grid = palette[8];  

            var rest = palette[7];   
            var future = palette[3]; 

            if (!HasUsableContrast(rest, future))
            {
                rest = WidgetTheme.RestColor(context, "Default");
                future = WidgetTheme.FutureColor(context, "Default");
            }

            return new Result(accent, grid, background, rest, future);
        }

        static bool HasUsableContrast(int a, int b) =>
            Math.Abs(RelativeLuminance(a) - RelativeLuminance(b)) > 0.12;

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