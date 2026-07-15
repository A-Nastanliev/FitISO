using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace FitISO.Maui
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void AttachBaseContext(Context @base)
        {
            var configuration = new Android.Content.Res.Configuration(@base.Resources.Configuration);
            configuration.FontScale = 1.0f;

            var context = @base.CreateConfigurationContext(configuration);
            base.AttachBaseContext(context);
        }
    }
}
