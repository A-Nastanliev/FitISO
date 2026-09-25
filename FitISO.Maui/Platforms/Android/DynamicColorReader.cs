#if ANDROID
using Android.Content;
using AndroidX.Core.Content;
#endif

namespace FitISO.Maui.Platforms.Android
{
    public static class DynamicColorReader
    {
        static readonly string[] TonalSteps =
        {
            "system_accent1_50",  
            "system_accent1_100",
            "system_accent1_200",
            "system_accent1_300",
            "system_accent1_400",
            "system_accent1_500",
            "system_accent1_600",
            "system_accent1_700",
            "system_accent1_800",
            "system_accent1_900",
            "system_accent1_1000", 
        };

#if ANDROID
        public static bool IsAvailable =>
            OperatingSystem.IsAndroidVersionAtLeast(31);

        public static int[]? ReadAccentTonalPalette(Context context)
        {
            if (!IsAvailable)
                return null;

            var resources = context.Resources;
            if (resources is null)
                return null;

            var result = new int[TonalSteps.Length];

            for (var i = 0; i < TonalSteps.Length; i++)
            {
                var resId = resources.GetIdentifier(TonalSteps[i], "color", "android");
                if (resId == 0)
                    return null; 

                try
                {
                    result[i] = ContextCompat.GetColor(context, resId);
                }
                catch
                {
                    return null;
                }
            }

            return result;
        }
#else
        public static bool IsAvailable => false;
        public static int[]? ReadAccentTonalPalette(object context) => null;
#endif
    }
}