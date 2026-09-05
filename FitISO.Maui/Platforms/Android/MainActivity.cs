using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using FitISO.Maui.Platforms.Android;
using FitISO.Maui.Services;

namespace FitISO.Maui
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [MetaData("android.app.shortcuts", Resource = "@xml/shortcuts")]
    [IntentFilter(new[] { ActionOpenHistory }, Categories = new[] { Intent.CategoryDefault })]
    public class MainActivity : MauiAppCompatActivity
    {
        public const string ActionOpenHistory = "com.companyname.fitiso.maui.shortcut.OPEN_HISTORY";

        protected override void AttachBaseContext(Context @base)
        {
            var configuration = new Android.Content.Res.Configuration(@base.Resources.Configuration);
            configuration.FontScale = 1.0f;

            var context = @base.CreateConfigurationContext(configuration);
            base.AttachBaseContext(context);
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            AppShell.PendingRoute = Intent?.Action == ActionOpenHistory ? "history" : null;

            base.OnCreate(savedInstanceState);
            HandleWidgetIntent(Intent);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);

            if (intent?.Action == ActionOpenHistory)
            {
                intent.SetAction(Intent.ActionMain);
                _ = Shell.Current?.GoToAsync("//history");
            }

            HandleWidgetIntent(intent);
        }

        static void HandleWidgetIntent(Intent? intent)
        {
            if (intent?.Action != FavouriteWorkoutStartWidgetProvider.ActionStartWorkout)
                return;

            intent.SetAction(Intent.ActionMain);

            var service = IPlatformApplication.Current?.Services.GetService<FavouriteWorkoutTemplateService>();
            if (service is null)
                return;

            _ = service.TryStartFavouriteWorkoutAsync();
        }
    }
}
