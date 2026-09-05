using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using FitISO.Maui.Models;
using System.Text.Json;

namespace FitISO.Maui.Platforms.Android
{
    [BroadcastReceiver(Label = "Favourite Workout", Exported = false)]
    [IntentFilter(new[] { AppWidgetManager.ActionAppwidgetUpdate })]
    [MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/favourite_workout_start_widget_provider")]
    public class FavouriteWorkoutStartWidgetProvider : AppWidgetProvider
    {
        public const string ActionRefresh = "com.fitiso.maui.widget.FAVOURITE_WORKOUT_START_REFRESH";
        public const string ActionStartWorkout = "com.fitiso.maui.widget.START_FAVOURITE_WORKOUT";
        public const string PrefsName = "FitISO.FavouriteWorkoutWidget";
        public const string SnapshotKey = "favourite_workout_snapshot_json";
        const int DetailedModeMinHeightDp = 110;

        const int PlayIconArgb = unchecked((int)0xFF212121);
        const int DefaultBackgroundArgb = unchecked((int)0xFF1E1E1E);
        const int DefaultAccentArgb = unchecked((int)0xFFCD5C5C);

        public override void OnReceive(Context? context, Intent? intent)
        {
            base.OnReceive(context, intent);

            if (context is null || intent?.Action != ActionRefresh)
                return;

            var manager = AppWidgetManager.GetInstance(context);
            var ids = manager.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(FavouriteWorkoutStartWidgetProvider))));
            OnUpdate(context, manager, ids);
        }

        public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
        {
            if (context is null || appWidgetManager is null || appWidgetIds is null || appWidgetIds.Length == 0)
                return;

            foreach (var widgetId in appWidgetIds)
                UpdateWidget(context, appWidgetManager, widgetId);
        }

        public override void OnAppWidgetOptionsChanged(Context? context, AppWidgetManager? appWidgetManager, int appWidgetId, Bundle? newOptions)
        {
            base.OnAppWidgetOptionsChanged(context, appWidgetManager, appWidgetId, newOptions);

            if (context is null || appWidgetManager is null)
                return;

            UpdateWidget(context, appWidgetManager, appWidgetId);
        }

        static void UpdateWidget(Context context, AppWidgetManager appWidgetManager, int widgetId)
        {
            var snapshot = ReadSnapshot(context);
            var detailed = ShouldShowDetailed(context, appWidgetManager.GetAppWidgetOptions(widgetId));

            var layoutId = detailed ? Resource.Layout.favourite_workout_start_widget_layout_detailed
                                     : Resource.Layout.favourite_workout_start_widget_layout;

            var views = new RemoteViews(context.PackageName, layoutId);
            ApplyToViews(context, views, snapshot, widgetId, detailed);
            appWidgetManager.UpdateAppWidget(widgetId, views);

            if (detailed)
            {
#pragma warning disable CA1422
                appWidgetManager.NotifyAppWidgetViewDataChanged(new[] { widgetId }, Resource.Id.widget_exercise_list);
#pragma warning restore CA1422
            }
        }

        static bool ShouldShowDetailed(Context context, Bundle? options)
        {
            if (options is null)
                return false;

            var isPortrait = context.Resources?.Configuration?.Orientation == global::Android.Content.Res.Orientation.Portrait;

            var heightDp = isPortrait
                ? options.GetInt(AppWidgetManager.OptionAppwidgetMinHeight, 0)
                : options.GetInt(AppWidgetManager.OptionAppwidgetMaxHeight, 0);

            return heightDp >= DetailedModeMinHeightDp;
        }

        static Workout? ReadSnapshot(Context context)
        {
            var prefs = context.GetSharedPreferences(PrefsName, FileCreationMode.Private);
            var json = prefs?.GetString(SnapshotKey, null);
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<Workout>(json);
            }
            catch
            {
                return null;
            }
        }

        static void ApplyToViews(Context context, RemoteViews views, Workout? snapshot, int widgetId, bool detailed)
        {
            var themePrefs = context.GetSharedPreferences(FavouriteExerciseHistoryWidgetProvider.PrefsName, FileCreationMode.Private);
            var backgroundArgb = themePrefs?.GetInt(FavouriteExerciseHistoryWidgetProvider.BackgroundColorKey, DefaultBackgroundArgb) ?? DefaultBackgroundArgb;
            var accentArgb = themePrefs?.GetInt(FavouriteExerciseHistoryWidgetProvider.AccentColorKey, DefaultAccentArgb) ?? DefaultAccentArgb;

            ApplyTint(views, Resource.Id.widget_root, backgroundArgb);

            var name = snapshot?.Name;

            if (string.IsNullOrEmpty(name))
            {
                views.SetViewVisibility(Resource.Id.widget_title, ViewStates.Gone);
                views.SetViewVisibility(Resource.Id.widget_start_button, ViewStates.Gone);
                if (detailed)
                    views.SetViewVisibility(Resource.Id.widget_exercise_list, ViewStates.Gone);
                views.SetViewVisibility(Resource.Id.widget_empty_state, ViewStates.Visible);
                return;
            }

            views.SetViewVisibility(Resource.Id.widget_empty_state, ViewStates.Gone);
            views.SetViewVisibility(Resource.Id.widget_title, ViewStates.Visible);
            views.SetViewVisibility(Resource.Id.widget_start_button, ViewStates.Visible);
            views.SetTextViewText(Resource.Id.widget_title, name);

            ApplyTint(views, Resource.Id.widget_start_button, accentArgb);
            views.SetInt(Resource.Id.widget_start_button, "setColorFilter", PlayIconArgb);

            var launchIntent = new Intent(context, typeof(MainActivity));
            launchIntent.SetAction(ActionStartWorkout);
            launchIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);

            var pendingIntent = PendingIntent.GetActivity(
                context,
                widgetId,
                launchIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            views.SetOnClickPendingIntent(Resource.Id.widget_start_button, pendingIntent);

            if (!detailed)
                return;

            views.SetViewVisibility(Resource.Id.widget_exercise_list, ViewStates.Visible);

            var adapterIntent = new Intent(context, typeof(FavouriteWorkoutExercisesRemoteViewsService));
            adapterIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, widgetId);
            adapterIntent.SetData(global::Android.Net.Uri.Parse($"widget://favourite-workout-exercises/{widgetId}"));

#pragma warning disable CA1422
            views.SetRemoteAdapter(Resource.Id.widget_exercise_list, adapterIntent);
#pragma warning restore CA1422
        }

        static void ApplyTint(RemoteViews views, int viewId, int argb)
        {
            var androidColor = new global::Android.Graphics.Color(argb);

            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                views.SetColorStateList(viewId, "setBackgroundTintList",
                    global::Android.Content.Res.ColorStateList.ValueOf(androidColor));
            }
            else
            {
                views.SetInt(viewId, "setBackgroundColor", argb);
            }
        }
    }
}