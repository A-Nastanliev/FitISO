using Android.Content;
using Android.Content.Res;

namespace FitISO.Maui.Platforms.Android
{
    public static class DynamicWidgetColors
    {
        public record Result(int Accent, int Grid, int Background, int Rest, int Future);

        public static Result? Compute(Context context)
        {
            var primary = TonalPalette.Read(context, "system_accent1");
            var neutral = TonalPalette.Read(context, "system_neutral1");
            var neutralVariant = TonalPalette.Read(context, "system_neutral2");

            if (primary is null || neutral is null || neutralVariant is null)
                return null;

            return IsNight(context)
                ? Dark(primary, neutral, neutralVariant)
                : Light(primary, neutral, neutralVariant);
        }

        static Result Dark(TonalPalette primary, TonalPalette neutral, TonalPalette neutralVariant) => new(
            Accent: primary.Tone(80),
            Grid: neutralVariant.Tone(30),
            Background: neutral.Tone(10),
            Rest: neutralVariant.Tone(30),
            Future: neutral.Tone(20));

        static Result Light(TonalPalette primary, TonalPalette neutral, TonalPalette neutralVariant) => new(
            Accent: primary.Tone(40),
            Grid: neutralVariant.Tone(80),
            Background: neutral.Tone(95),
            Rest: neutralVariant.Tone(80),
            Future: neutral.Tone(90));

        static bool IsNight(Context context)
        {
            var uiMode = context.Resources?.Configuration?.UiMode ?? 0;
            return (uiMode & UiMode.NightMask) == UiMode.NightYes;
        }
    }
}