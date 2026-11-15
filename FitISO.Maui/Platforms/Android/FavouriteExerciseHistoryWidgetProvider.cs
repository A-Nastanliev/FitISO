using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using FitISO.Maui.Models;
using FitISO.Maui.Rendering;
using SkiaSharp;
using System.Text.Json;

namespace FitISO.Maui.Platforms.Android
{
    [BroadcastReceiver(Label = "Favourite Exercise History", Exported = false)]
    [IntentFilter(new[] { AppWidgetManager.ActionAppwidgetUpdate, WidgetTheme.ActionThemeChanged })]
    [MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/favourite_exercise_history_widget_provider")]
    public class FavouriteExerciseHistoryWidgetProvider : AppWidgetProvider
    {
        public const string ActionRefresh = "com.fitiso.maui.widget.FAVOURITE_EXERCISE_HISTORY_REFRESH";
        public const string PrefsName = "FitISO.FavouriteExerciseWidget";
        public const string SnapshotKey = "snapshot";

        const int WidgetWidthDp = 180;
        const int WidgetHeightDp = 110;
        const int WidgetPaddingDp = 10;
        const int TitleBlockDp = 28;

        public override void OnReceive(Context? context, Intent? intent)
        {
            base.OnReceive(context, intent);

            if (context is null || (intent?.Action != ActionRefresh && intent?.Action != WidgetTheme.ActionThemeChanged))
                return;

            var manager = AppWidgetManager.GetInstance(context);
            var ids = manager.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(FavouriteExerciseHistoryWidgetProvider))));
            OnUpdate(context, manager, ids);
        }

        public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
        {
            if (context is null || appWidgetManager is null || appWidgetIds is null || appWidgetIds.Length == 0)
                return;

            var snapshot = ReadSnapshot(context);
            var density = context.Resources!.DisplayMetrics!.Density;

            foreach (var widgetId in appWidgetIds)
            {
                var options = appWidgetManager.GetAppWidgetOptions(widgetId);
                var (widgetWidthDp, widgetHeightDp) = GetWidgetSizeDp(context, options);

                var chartWidthPx = (int)((widgetWidthDp - 2 * WidgetPaddingDp) * density);
                var chartHeightPx = (int)((widgetHeightDp - 2 * WidgetPaddingDp - TitleBlockDp) * density);

                var views = new RemoteViews(context.PackageName, Resource.Layout.favourite_exercise_history_widget_layout);

                ApplyToViews(context, views, snapshot, chartWidthPx, chartHeightPx);
                appWidgetManager.UpdateAppWidget(widgetId, views);
            }
        }

        public override void OnAppWidgetOptionsChanged(Context? context, AppWidgetManager? appWidgetManager, int appWidgetId, Bundle? newOptions)
        {
            base.OnAppWidgetOptionsChanged(context, appWidgetManager, appWidgetId, newOptions);
            OnUpdate(context, appWidgetManager, new[] { appWidgetId });
        }

        static (int WidthDp, int HeightDp) GetWidgetSizeDp(Context context, Bundle? options)
        {
            var portrait = context.Resources!.Configuration!.Orientation == global::Android.Content.Res.Orientation.Portrait;

            var widthKey = portrait ? AppWidgetManager.OptionAppwidgetMinWidth : AppWidgetManager.OptionAppwidgetMaxWidth;
            var heightKey = portrait ? AppWidgetManager.OptionAppwidgetMaxHeight : AppWidgetManager.OptionAppwidgetMinHeight;

            return (OptionOrDefault(options, widthKey, WidgetWidthDp),
                    OptionOrDefault(options, heightKey, WidgetHeightDp));
        }

        static int OptionOrDefault(Bundle? options, string key, int fallback)
        {
            var value = options?.GetInt(key, fallback) ?? fallback;
            return value > 0 ? value : fallback;
        }

        static Exercise? ReadSnapshot(Context context)
        {
            var prefs = context.GetSharedPreferences(PrefsName, FileCreationMode.Private);
            var json = prefs?.GetString(SnapshotKey, null);
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

        static void ApplyToViews(Context context, RemoteViews views, Exercise? snapshot, int widthPx, int heightPx)
        {
            var themePrefs = WidgetTheme.Prefs(context);
            var backgroundArgb = WidgetTheme.BackgroundColor(context, themePrefs);
            views.ApplyTint(Resource.Id.widget_root, backgroundArgb);

            if (snapshot is null || snapshot.History.Count == 0)
            {
                ShowEmptyState(views, "No favourite exercise yet");
                return;
            }

            var accent = WidgetTheme.ToSkColor(WidgetTheme.AccentColor(context, themePrefs));
            var gridColor = WidgetTheme.ToSkColor(WidgetTheme.GridColor(context, themePrefs));

            var density = context.Resources!.DisplayMetrics!.Density;

            using var skBitmap = ExerciseChartDrawer.Draw(snapshot.History, Math.Max(widthPx, 1), Math.Max(heightPx, 1), accent, gridColor, density);
            using var image = SKImage.FromBitmap(skBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = data.AsStream();
            using var androidBitmap = global::Android.Graphics.BitmapFactory.DecodeStream(stream);

            views.SetTextViewText(Resource.Id.widget_title, snapshot.Name);
            views.SetViewVisibility(Resource.Id.widget_empty_state, ViewStates.Gone);
            views.SetViewVisibility(Resource.Id.widget_chart, ViewStates.Visible);
            views.SetImageViewBitmap(Resource.Id.widget_chart, androidBitmap);
        }

        static void ShowEmptyState(RemoteViews views, string title)
        {
            views.SetTextViewText(Resource.Id.widget_title, title);
            views.SetViewVisibility(Resource.Id.widget_chart, ViewStates.Gone);
            views.SetViewVisibility(Resource.Id.widget_empty_state, ViewStates.Visible);
        }
    }
}