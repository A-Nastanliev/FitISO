using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Views;
using Android.Widget;
using FitISO.Data;
using FitISO.Maui.Services;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using System.Text.Json;

namespace FitISO.Maui.Platforms.Android
{
    [BroadcastReceiver(Label = "Monthly Heatmap", Exported = false)]
    [IntentFilter(new[] { AppWidgetManager.ActionAppwidgetUpdate })]
    [MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/monthly_heatmap_widget_provider")]
    public class MonthlyHeatmapWidgetProvider : AppWidgetProvider
    {
        public const string ActionRefresh = "com.fitiso.maui.widget.MONTHLY_HEATMAP_REFRESH";

        const int DefaultWorkoutArgb = unchecked((int)0xFFCD5C5C);
        const int DefaultRestArgb = unchecked((int)0xFF3A3A3A);
        const int DefaultFutureArgb = unchecked((int)0xFF232323);
        const int DefaultBackgroundArgb = unchecked((int)0xFF1E1E1E);

        public override void OnReceive(Context? context, Intent? intent)
        {
            base.OnReceive(context, intent);

            if (context is null || intent?.Action != ActionRefresh)
                return;

            var manager = AppWidgetManager.GetInstance(context);
            var ids = manager.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(MonthlyHeatmapWidgetProvider))));
            OnUpdate(context, manager, ids);
        }

        public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
        {
            if (context is null || appWidgetManager is null || appWidgetIds is null || appWidgetIds.Length == 0)
                return;

            _ = UpdateAllAsync(context, appWidgetManager, appWidgetIds);
        }

        static async Task UpdateAllAsync(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            await EnsureCacheIsForCurrentMonthAsync();

            foreach (var widgetId in appWidgetIds)
                UpdateWidget(context, appWidgetManager, widgetId);
        }

        static async Task EnsureCacheIsForCurrentMonthAsync()
        {
            var now = DateTime.Now;
            var prefs = global::Android.App.Application.Context.GetSharedPreferences(
                MonthlyHeatmapService.PrefsName, FileCreationMode.Private);

            var cachedYear = prefs?.GetInt(MonthlyHeatmapService.CachedYearKey, 0) ?? 0;
            var cachedMonth = prefs?.GetInt(MonthlyHeatmapService.CachedMonthKey, 0) ?? 0;

            if (cachedYear == now.Year && cachedMonth == now.Month)
                return; 

            var service = IPlatformApplication.Current?.Services.GetService<MonthlyHeatmapService>();
            if (service is not null)
            {
                await service.RebuildCacheForCurrentMonthAsync();
                return;
            }

            var factory = IPlatformApplication.Current?.Services.GetService<IDbContextFactory<FitDbContext>>();
            if (factory is null) return;

            using var dbContext = factory.CreateDbContext();
            var starts = await dbContext.Workouts
                .AsNoTracking()
                .Where(w => w.StartTime != null && w.EndTime != null)
                .Select(w => w.StartTime!.Value)
                .ToListAsync();

            var days = starts
                .Select(s => (s.Kind == DateTimeKind.Utc ? s : DateTime.SpecifyKind(s, DateTimeKind.Utc)).ToLocalTime())
                .Where(local => local.Year == now.Year && local.Month == now.Month)
                .Select(local => local.Day);

            using var editor = prefs!.Edit();
            editor!.PutInt(MonthlyHeatmapService.CachedYearKey, now.Year);
            editor!.PutInt(MonthlyHeatmapService.CachedMonthKey, now.Month);
            editor!.PutString(MonthlyHeatmapService.CachedDaysKey, JsonSerializer.Serialize(new HashSet<int>(days)));
            editor!.Apply();
        }

        static void UpdateWidget(Context context, AppWidgetManager appWidgetManager, int widgetId)
        {
            var views = new RemoteViews(context.PackageName, Resource.Layout.monthly_heatmap_widget_layout);

            var prefs = context.GetSharedPreferences(MonthlyHeatmapService.PrefsName, FileCreationMode.Private);
            var year = prefs?.GetInt(MonthlyHeatmapService.CachedYearKey, 0) ?? 0;
            var month = prefs?.GetInt(MonthlyHeatmapService.CachedMonthKey, 0) ?? 0;
            var daysJson = prefs?.GetString(MonthlyHeatmapService.CachedDaysKey, null);

            var backgroundArgb = prefs?.GetInt(MonthlyHeatmapService.BackgroundColorKey, DefaultBackgroundArgb) ?? DefaultBackgroundArgb;
            ApplyBackgroundTint(views, backgroundArgb);

            HashSet<int> workoutDays;
            try
            {
                workoutDays = string.IsNullOrEmpty(daysJson)
                    ? new HashSet<int>()
                    : JsonSerializer.Deserialize<HashSet<int>>(daysJson) ?? new HashSet<int>();
            }
            catch
            {
                workoutDays = new HashSet<int>();
            }

            var now = DateTime.Now;
            var isCurrentMonth = year == now.Year && month == now.Month;

            if (!isCurrentMonth || year == 0)
            {
                year = now.Year;
                month = now.Month;
                workoutDays = new HashSet<int>();
            }

            var daysInMonth = DateTime.DaysInMonth(year, month);
            var firstDayOfWeek = new DateTime(year, month, 1).DayOfWeek;
            var today = (year == now.Year && month == now.Month) ? now.Day : daysInMonth;

            var density = context.Resources!.DisplayMetrics!.Density;
            const int WidgetWidthDp = 250;
            const int WidgetHeightDp = 130;
            var w = (int)(WidgetWidthDp * density);
            var h = (int)(WidgetHeightDp * density);

            var workoutColor = ReadColor(prefs, MonthlyHeatmapService.WorkoutColorKey, DefaultWorkoutArgb);
            var restColor = ReadColor(prefs, MonthlyHeatmapService.RestColorKey, DefaultRestArgb);
            var futureColor = ReadColor(prefs, MonthlyHeatmapService.FutureColorKey, DefaultFutureArgb);

            using var skBitmap = HeatmapChartDrawer.Draw(
                workoutDays, today, daysInMonth, firstDayOfWeek,
                workoutColor, restColor, futureColor, w, h);
            using var image = SKImage.FromBitmap(skBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = data.AsStream();
            using var androidBitmap = global::Android.Graphics.BitmapFactory.DecodeStream(stream);

            views.SetImageViewBitmap(Resource.Id.widget_heatmap, androidBitmap);
            appWidgetManager.UpdateAppWidget(widgetId, views);
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
    }
}