using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Views;
using Android.Widget;
using FitISO.Maui.Models;
using System.Text.Json;

namespace FitISO.Maui.Platforms.Android
{
    [BroadcastReceiver(Label = "Favourite Exercise Best Set", Exported = false)]
    [IntentFilter(new[] { AppWidgetManager.ActionAppwidgetUpdate })]
    [MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/favourite_exercise_best_set_widget_provider")]
    public class FavouriteExerciseBestSetWidgetProvider : AppWidgetProvider
    {
        public const string ActionRefresh = "com.fitiso.maui.widget.FAVOURITE_EXERCISE_BEST_SET_REFRESH";
        const int DefaultBackgroundArgb = unchecked((int)0xFF1E1E1E);
        const int DefaultAccentArgb = unchecked((int)0xFFCD5C5C);

        public override void OnReceive(Context? context, Intent? intent)
        {
            base.OnReceive(context, intent);

            if (context is null || intent?.Action != ActionRefresh)
                return;

            var manager = AppWidgetManager.GetInstance(context);
            var ids = manager.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(FavouriteExerciseBestSetWidgetProvider))));
            OnUpdate(context, manager, ids);
        }

        public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
        {
            if (context is null || appWidgetManager is null || appWidgetIds is null || appWidgetIds.Length == 0)
                return;

            var snapshot = ReadSnapshot(context);

            foreach (var widgetId in appWidgetIds)
            {
                var views = new RemoteViews(context.PackageName, Resource.Layout.favourite_exercise_best_set_widget_layout);

                ApplyToViews(context, views, snapshot);
                appWidgetManager.UpdateAppWidget(widgetId, views);
            }
        }

        static Exercise? ReadSnapshot(Context context)
        {
            var prefs = context.GetSharedPreferences(FavouriteExerciseHistoryWidgetProvider.PrefsName, FileCreationMode.Private);
            var json = prefs?.GetString(FavouriteExerciseHistoryWidgetProvider.SnapshotKey, null);
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<Exercise>(json);
            }
            catch
            {
                return null;
            }
        }

        static void ApplyToViews(Context context, RemoteViews views, Exercise? snapshot)
        {
            var prefs = context.GetSharedPreferences(FavouriteExerciseHistoryWidgetProvider.PrefsName, FileCreationMode.Private);
            var backgroundArgb = prefs?.GetInt(FavouriteExerciseHistoryWidgetProvider.BackgroundColorKey, DefaultBackgroundArgb) ?? DefaultBackgroundArgb;
            var androidColor = new global::Android.Graphics.Color(backgroundArgb);

            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                views.SetColorStateList(Resource.Id.widget_root, "setBackgroundTintList",
                    global::Android.Content.Res.ColorStateList.ValueOf(androidColor));
            }
            else
            {
                views.SetInt(Resource.Id.widget_root, "setBackgroundColor", backgroundArgb);
            }

            var accentArgb = ReadAccentColor(prefs);
            views.SetInt(Resource.Id.widget_trophy, "setColorFilter", accentArgb);

            var bestSet = snapshot?.BestSet;
            var hasBestSet = bestSet is not null && ((bestSet.Weight ?? 0) > 0 || (bestSet.Reps ?? 0) > 0);

            if (snapshot is null || !hasBestSet)
            {
                ShowEmptyState(views);
                return;
            }

            views.SetTextViewText(Resource.Id.widget_title, snapshot.Name);
            views.SetTextViewText(Resource.Id.widget_best_set, FormatBestSet(bestSet!));
            views.SetTextColor(Resource.Id.widget_best_set, new global::Android.Graphics.Color(accentArgb));
            views.SetViewVisibility(Resource.Id.widget_empty_state, ViewStates.Gone);
            views.SetViewVisibility(Resource.Id.widget_content, ViewStates.Visible);
            views.SetViewVisibility(Resource.Id.widget_best_set, ViewStates.Visible);
        }

        static int ReadAccentColor(ISharedPreferences? prefs) =>
            prefs?.GetInt(FavouriteExerciseHistoryWidgetProvider.AccentColorKey, DefaultAccentArgb) ?? DefaultAccentArgb;

        static string FormatBestSet(Set bestSet)
        {
            var hasWeight = bestSet.Weight is > 0;
            var hasReps = bestSet.Reps is > 0;

            if (hasWeight && hasReps)
                return $"{bestSet.Weight:0.##} kg × {bestSet.Reps:0.##}";

            if (hasReps)
                return $"{bestSet.Reps:0.##} reps";

            return $"{bestSet.Weight:0.##} kg";
        }

        static void ShowEmptyState(RemoteViews views)
        {
            views.SetViewVisibility(Resource.Id.widget_content, ViewStates.Gone);
            views.SetViewVisibility(Resource.Id.widget_empty_state, ViewStates.Visible);
        }
    }
}