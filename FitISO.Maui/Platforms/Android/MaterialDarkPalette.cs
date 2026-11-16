using Android.Content;

namespace FitISO.Maui.Platforms.Android
{
    public sealed record MaterialDarkPalette(
        int Gray100, int Gray200, int Gray300, int Gray400, int Gray500, int Gray600,
        int Gray700, int Gray800, int Gray900, int Gray950, int Accent)
    {
        public static MaterialDarkPalette? Read(Context context)
        {
            var primary = TonalPalette.Read(context, "system_accent1");
            var neutral = TonalPalette.Read(context, "system_neutral1");
            var neutralVariant = TonalPalette.Read(context, "system_neutral2");

            if (primary is null || neutral is null || neutralVariant is null)
                return null;

            return new MaterialDarkPalette(
                Gray100: neutral.Tone(90),
                Gray200: neutralVariant.Tone(80),
                Gray300: neutralVariant.Tone(70),
                Gray400: neutralVariant.Tone(60),
                Gray500: neutralVariant.Tone(50),
                Gray600: neutralVariant.Tone(40),
                Gray700: neutralVariant.Tone(30),
                Gray800: neutral.Tone(20),
                Gray900: neutral.Tone(20),
                Gray950: neutral.Tone(10),
                Accent: primary.Tone(80));
        }
    }
}