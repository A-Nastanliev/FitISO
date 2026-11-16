using Android.Content;
using AndroidX.Core.Content;

namespace FitISO.Maui.Platforms.Android
{
    public sealed class TonalPalette
    {
        static readonly Dictionary<int, string> ResourceSuffixByTone = new()
        {
            [100] = "0",
            [99] = "10",
            [95] = "50",
            [90] = "100",
            [80] = "200",
            [70] = "300",
            [60] = "400",
            [50] = "500",
            [40] = "600",
            [30] = "700",
            [20] = "800",
            [10] = "900",
            [0] = "1000",
        };

        readonly Dictionary<int, int> _colors;

        TonalPalette(Dictionary<int, int> colors) => _colors = colors;

        public int Tone(int tone) => _colors[tone];

        public static TonalPalette? Read(Context context, string family)
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(31))
                return null;

            var resources = context.Resources;
            if (resources is null)
                return null;

            var colors = new Dictionary<int, int>();

            foreach (var (tone, suffix) in ResourceSuffixByTone)
            {
                var resId = resources.GetIdentifier($"{family}_{suffix}", "color", "android");
                if (resId == 0)
                    return null;

                try
                {
                    colors[tone] = ContextCompat.GetColor(context, resId);
                }
                catch
                {
                    return null;
                }
            }

            return new TonalPalette(colors);
        }
    }
}