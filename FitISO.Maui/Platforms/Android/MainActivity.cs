using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Graphics.Drawables;
using Android.OS;
using AndroidX.Core.Content;
using FitISO.Maui.Platforms.Android;
using FitISO.Maui.Services;
using FitISO.Maui.ViewModels;
using System.ComponentModel;

namespace FitISO.Maui
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | 
        ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
        SupportsPictureInPicture = true)]
    [MetaData("android.app.shortcuts", Resource = "@xml/shortcuts")]
    [IntentFilter(new[] { ActionOpenHistory }, Categories = new[] { Intent.CategoryDefault })]
    public class MainActivity : MauiAppCompatActivity
    {
        public const string ActionOpenHistory = "com.companyname.fitiso.maui.shortcut.OPEN_HISTORY";

        internal const string ActionPipToggleRest = "com.companyname.fitiso.maui.pip.TOGGLE_REST";
        internal const string ActionPipResetRest = "com.companyname.fitiso.maui.pip.RESET_REST";
        const int RequestCodeToggle = 9001;
        const int RequestCodeReset = 9002;

        RestPipActionReceiver? pipReceiver;
        ActiveWorkoutViewModel? activeWorkoutViewModel;

        protected override void AttachBaseContext(Context @base)
        {
            var configuration = new Configuration(@base.Resources.Configuration);
            configuration.FontScale = 1.0f;

            var context = @base.CreateConfigurationContext(configuration);
            base.AttachBaseContext(context);
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            AppShell.PendingRoute = Intent?.Action == ActionOpenHistory ? "history" : null;

            base.OnCreate(savedInstanceState);
            HandleWidgetIntent(Intent);

            activeWorkoutViewModel = IPlatformApplication.Current?.Services.GetService<ActiveWorkoutViewModel>();

            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                pipReceiver = new RestPipActionReceiver(OnPipToggleRequested, OnPipResetRequested);
                var filter = new IntentFilter();
                filter.AddAction(ActionPipToggleRest);
                filter.AddAction(ActionPipResetRest);
                ContextCompat.RegisterReceiver(this, pipReceiver, filter, ContextCompat.ReceiverNotExported);
            }

            if (activeWorkoutViewModel is not null)
                activeWorkoutViewModel.PropertyChanged += ActiveWorkoutViewModel_PropertyChanged;
        }

        protected override void OnDestroy()
        {
            if (activeWorkoutViewModel is not null)
                activeWorkoutViewModel.PropertyChanged -= ActiveWorkoutViewModel_PropertyChanged;

            if (pipReceiver is not null)
                UnregisterReceiver(pipReceiver);

            base.OnDestroy();
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

        protected override void OnUserLeaveHint()
        {
            base.OnUserLeaveHint();

            if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;
            if (!ActiveWorkoutState.Instance.IsActiveWorkoutPageVisible) return;
            if (activeWorkoutViewModel?.RestStartTime is null) return;

            EnterPictureInPictureMode(BuildPipParams());
        }

        public override void OnPictureInPictureModeChanged(bool isInPictureInPictureMode, Configuration? newConfig)
        {
            base.OnPictureInPictureModeChanged(isInPictureInPictureMode, newConfig);
            ActiveWorkoutState.Instance.IsInPipMode = isInPictureInPictureMode;
        }

        void ActiveWorkoutViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(26) || !IsInPictureInPictureMode) return;

            if (e.PropertyName is nameof(ActiveWorkoutViewModel.RestIsStopped) or nameof(ActiveWorkoutViewModel.RestStartTime))
                SetPictureInPictureParams(BuildPipParams());
        }

        PictureInPictureParams BuildPipParams()
        {
            var builder = new PictureInPictureParams.Builder()
                .SetAspectRatio(new global::Android.Util.Rational(2, 1));

            var actions = new List<RemoteAction>();

            if (activeWorkoutViewModel?.RestIsStopped == true)
            {
                actions.Add(BuildAction(Resource.Drawable.ic_pip_resume, "Resume", ActionPipToggleRest, RequestCodeToggle));
                actions.Add(BuildAction(Resource.Drawable.ic_pip_reset, "Reset", ActionPipResetRest, RequestCodeReset));
            }
            else
            {
                actions.Add(BuildAction(Resource.Drawable.ic_pip_pause, "Stop", ActionPipToggleRest, RequestCodeToggle));
            }

            builder.SetActions(actions);
            return builder.Build();
        }

        RemoteAction BuildAction(int iconRes, string title, string action, int requestCode)
        {
            var icon = Icon.CreateWithResource(this, iconRes);
            var intent = new Intent(action).SetPackage(PackageName);
            var pendingIntent = PendingIntent.GetBroadcast(this, requestCode, intent,
                PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
            return new RemoteAction(icon, title, title, pendingIntent!);
        }

        void OnPipToggleRequested()
        {
            if (activeWorkoutViewModel is null) return;

            if (activeWorkoutViewModel.RestIsStopped)
                activeWorkoutViewModel.ResumeRestCommand.Execute(null);
            else
                activeWorkoutViewModel.StopRestCommand.Execute(null);
        }

        void OnPipResetRequested() => activeWorkoutViewModel?.ManualResetRestCommand.Execute(null);
    }
}