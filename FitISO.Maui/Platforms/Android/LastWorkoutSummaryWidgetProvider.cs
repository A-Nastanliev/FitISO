using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Net;
using Android.Views;
using Android.Widget;
using FitISO.Maui.Models;
using System.Text.Json;

namespace FitISO.Maui.Platforms.Android
{
    [BroadcastReceiver(Label = "Last Workout Summary", Exported = false)]
    [IntentFilter(new[] { AppWidgetManager.ActionAppwidgetUpdate, WidgetTheme.ActionThemeChanged })]
    [MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/last_workout_summary_widget_provider")]
    public class LastWorkoutSummaryWidgetProvider : AppWidgetProvider
    {
        public const string ActionRefresh = "com.fitiso.maui.widget.LAST_WORKOUT_SUMMARY_REFRESH";
        public const string SnapshotKey = "last_workout_summary_json";

        public override void OnReceive(Context? context, Intent? intent)
        {
            base.OnReceive(context, intent);

            if (context is null || (intent?.Action != ActionRefresh && intent?.Action != WidgetTheme.ActionThemeChanged))
                return;

            var manager = AppWidgetManager.GetInstance(context);
            var ids = manager.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(LastWorkoutSummaryWidgetProvider))));
            OnUpdate(context, manager, ids);
        }

        public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
        {
            if (context is null || appWidgetManager is null || appWidgetIds is null || appWidgetIds.Length == 0)
                return;

            var workout = ReadSnapshot(context);
            var themePrefs = WidgetTheme.Prefs(context);
            var backgroundArgb = WidgetTheme.BackgroundColor(context, themePrefs);
            var accentArgb = WidgetTheme.AccentColor(context, themePrefs);

            foreach (var widgetId in appWidgetIds)
            {
                var views = new RemoteViews(context.PackageName, Resource.Layout.last_workout_summary_widget_layout);

                views.ApplyTint(Resource.Id.widget_root, backgroundArgb);
                views.SetTextColor(Resource.Id.widget_title, new global::Android.Graphics.Color(accentArgb));

                if (workout is null || workout.WorkoutExercises.Count == 0)
                {
                    views.SetViewVisibility(Resource.Id.widget_list, ViewStates.Gone);
                    views.SetViewVisibility(Resource.Id.widget_empty_state, ViewStates.Visible);
                    views.SetTextViewText(Resource.Id.widget_title, "Last Workout");
                    views.SetTextViewText(Resource.Id.widget_subtitle, string.Empty);
                    appWidgetManager.UpdateAppWidget(widgetId, views);
                    continue;
                }

                views.SetViewVisibility(Resource.Id.widget_list, ViewStates.Visible);
                views.SetViewVisibility(Resource.Id.widget_empty_state, ViewStates.Gone);
                views.SetTextViewText(Resource.Id.widget_title, workout.Name);
                views.SetTextViewText(Resource.Id.widget_subtitle,
                    workout.StartTime?.ToLocalTime().ToString("MMM d, HH:mm") ?? string.Empty);

                var adapterIntent = new Intent(context, typeof(LastWorkoutSummaryRemoteViewsService));
                adapterIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, widgetId);
                adapterIntent.SetData(global::Android.Net.Uri.Parse($"widget://last-workout-summary/{widgetId}"));

#pragma warning disable CA1422
                views.SetRemoteAdapter(Resource.Id.widget_list, adapterIntent);
#pragma warning restore CA1422
                views.SetEmptyView(Resource.Id.widget_list, Resource.Id.widget_empty_state);

                appWidgetManager.UpdateAppWidget(widgetId, views);
            }

#pragma warning disable CA1422
            appWidgetManager.NotifyAppWidgetViewDataChanged(appWidgetIds, Resource.Id.widget_list);
#pragma warning restore CA1422
        }

        static Workout? ReadSnapshot(Context context)
        {
            var prefs = context.GetSharedPreferences(FavouriteExerciseHistoryWidgetProvider.PrefsName, FileCreationMode.Private);
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
    }
}