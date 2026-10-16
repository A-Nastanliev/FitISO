using Android.App;
using Android.Content;
using AndroidX.Core.App;
using FitISO.Maui.Models;
using FitISO.Maui.ViewModels;
using System.ComponentModel;

namespace FitISO.Maui.Platforms.Android
{
    sealed class WorkoutNotificationManager
    {
        const string ChannelId = "active_workout";
        const int NotificationId = 4001;

        readonly Context context;
        readonly NotificationManager? notificationManager;
        ActiveWorkoutViewModel? viewModel;
        Workout? observedWorkout;

        public static WorkoutNotificationManager? Instance { get; private set; }

        WorkoutNotificationManager(Context context)
        {
            this.context = context;
            notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;
        }

        public static void Register(Context context)
        {
            if (Instance is not null) return;

            Instance = new WorkoutNotificationManager(context.ApplicationContext ?? context);
            Instance.Start();
        }

        void Start()
        {
            CreateChannel();

            viewModel = IPlatformApplication.Current?.Services.GetService<ActiveWorkoutViewModel>();

            ActiveWorkoutState.Instance.PropertyChanged += ActiveWorkoutState_PropertyChanged;

            if (viewModel is not null)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
                ObserveWorkout(viewModel.Workout);
            }

            Update();
        }

        void ActiveWorkoutState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ActiveWorkoutState.HasActiveWorkout))
                Update();
        }

        void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ActiveWorkoutViewModel.Workout)) return;

            ObserveWorkout(viewModel?.Workout);
            Update();
        }

        void Workout_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(Workout.Name) or nameof(Workout.StartTime))
                Update();
        }

        void ObserveWorkout(Workout? workout)
        {
            if (ReferenceEquals(observedWorkout, workout)) return;

            if (observedWorkout is not null)
                observedWorkout.PropertyChanged -= Workout_PropertyChanged;

            observedWorkout = workout;

            if (observedWorkout is not null)
                observedWorkout.PropertyChanged += Workout_PropertyChanged;
        }

        public void Update()
        {
            var workout = viewModel?.Workout;

            if (!ActiveWorkoutState.Instance.HasActiveWorkout || workout?.StartTime is not DateTime start)
            {
                notificationManager?.Cancel(NotificationId);
                return;
            }

            if (start.Kind != DateTimeKind.Utc)
                start = DateTime.SpecifyKind(start, DateTimeKind.Utc);

            var startMillis = new DateTimeOffset(start).ToUnixTimeMilliseconds();
            var title = string.IsNullOrWhiteSpace(workout.Name) ? "Workout" : workout.Name.Trim();

            var builder = new NotificationCompat.Builder(context, ChannelId)
                .SetSmallIcon(Resource.Drawable.ic_stat_workout)
                .SetContentTitle(title)
                .SetContentText("Workout in progress")
                .SetOngoing(true)
                .SetOnlyAlertOnce(true)
                .SetCategory(NotificationCompat.CategoryStatus)
                .SetVisibility(NotificationCompat.VisibilityPublic)
                .SetShowWhen(true)
                .SetUsesChronometer(true)
                .SetWhen(startMillis);

            var launchIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName!);
            if (launchIntent is not null)
            {
                var contentIntent = PendingIntent.GetActivity(context, 0, launchIntent,
                    PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
                builder.SetContentIntent(contentIntent);
            }

            var notification = builder.Build();
            if (notification is not null)
                notificationManager?.Notify(NotificationId, notification);
        }

        void CreateChannel()
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;

            var channel = new NotificationChannel(ChannelId, "Active workout", NotificationImportance.Low)
            {
                Description = "Shows the running time of your ongoing workout."
            };
            channel.SetShowBadge(false);

            notificationManager?.CreateNotificationChannel(channel);
        }
    }
}