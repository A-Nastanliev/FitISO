using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Views;
using Android.Widget;
using FitISO.Maui.Models;
using SkiaSharp;
using System.Text.Json;

namespace FitISO.Maui.Platforms.Android
{
    [BroadcastReceiver(Label = "Favourite Exercise", Exported = false)]
    [IntentFilter(new[] { AppWidgetManager.ActionAppwidgetUpdate })]
    [MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/favourite_exercise_widget_provider")]
    public class FavouriteExerciseWidgetProvider : AppWidgetProvider
    {
        public const string ActionRefresh = "com.fitiso.maui.widget.FAVOURITE_EXERCISE_REFRESH";
        public const string PrefsName = "FitISO.FavouriteExerciseWidget";
        public const string SnapshotKey = "snapshot";
        public const string AccentColorKey = "accent_color";
        public const string GridColorKey = "grid_color";
        public const string BackgroundColorKey = "background_color";

        const int DefaultAccentArgb = unchecked((int)0xFFCD5C5C);
        const int DefaultGridArgb = unchecked((int)0xFFFFFFFF);
        const int DefaultBackgroundArgb = unchecked((int)0xFF1E1E1E);

        const int WidgetWidthDp = 180;
        const int WidgetHeightDp = 110;

        public override void OnReceive(Context? context, Intent? intent)
        {
            base.OnReceive(context, intent);

            if (context is null || intent?.Action != ActionRefresh)
                return;

            var manager = AppWidgetManager.GetInstance(context);
            var ids = manager.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(FavouriteExerciseWidgetProvider))));
            OnUpdate(context, manager, ids);
        }

        public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
        {
            if (context is null || appWidgetManager is null || appWidgetIds is null || appWidgetIds.Length == 0)
                return;

            var snapshot = ReadSnapshot(context);
            var density = context.Resources!.DisplayMetrics!.Density;
            var widthPx = (int)(WidgetWidthDp * density);
            var heightPx = (int)(WidgetHeightDp * density);

            foreach (var widgetId in appWidgetIds)
            {
                var views = new RemoteViews(context.PackageName, Resource.Layout.favourite_exercise_widget_layout);

                ApplyToViews(context, views, snapshot, widthPx, heightPx);
                appWidgetManager.UpdateAppWidget(widgetId, views);
            }
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
            var prefs = context.GetSharedPreferences(PrefsName, FileCreationMode.Private);
            var backgroundArgb = prefs?.GetInt(BackgroundColorKey, DefaultBackgroundArgb) ?? DefaultBackgroundArgb;
            ApplyBackgroundTint(views, backgroundArgb);

            if (snapshot is null || snapshot.History.Count == 0)
            {
                ShowEmptyState(views, "No favourite exercise yet");
                return;
            }

            var accent = ReadColor(prefs, AccentColorKey, DefaultAccentArgb);
            var gridColor = ReadColor(prefs, GridColorKey, DefaultGridArgb);

            using var skBitmap = ExerciseChartDrawer.Draw(snapshot.History, Math.Max(widthPx, 1), Math.Max(heightPx, 1), accent, gridColor);
            using var image = SKImage.FromBitmap(skBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = data.AsStream();
            using var androidBitmap = global::Android.Graphics.BitmapFactory.DecodeStream(stream);

            views.SetTextViewText(Resource.Id.widget_title, snapshot.Name);
            views.SetViewVisibility(Resource.Id.widget_empty_state, ViewStates.Gone);
            views.SetViewVisibility(Resource.Id.widget_chart, ViewStates.Visible);
            views.SetImageViewBitmap(Resource.Id.widget_chart, androidBitmap);
        }

        static SKColor ReadColor(ISharedPreferences? prefs, string key, int defaultArgb)
        {
            var argb = prefs?.GetInt(key, defaultArgb) ?? defaultArgb;
            return new SKColor(
                (byte)((argb >> 16) & 0xFF),
                (byte)((argb >> 8) & 0xFF),
                (byte)(argb & 0xFF),
                (byte)((argb >> 24) & 0xFF));
        }

        static void ApplyBackgroundTint(RemoteViews views, int backgroundArgb)
        {
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
        }

        static void ShowEmptyState(RemoteViews views, string title)
        {
            views.SetTextViewText(Resource.Id.widget_title, title);
            views.SetViewVisibility(Resource.Id.widget_chart, ViewStates.Gone);
            views.SetViewVisibility(Resource.Id.widget_empty_state, ViewStates.Visible);
        }
    }
}