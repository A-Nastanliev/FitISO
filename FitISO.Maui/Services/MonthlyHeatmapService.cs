using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Services;
using System.Text.Json;
#if ANDROID
using Android.Content;
using FitISO.Maui.Platforms.Android;
#endif

namespace FitISO.Maui.Services
{
    public class MonthlyHeatmapService : IRecipient<WorkoutFinishedMessage>, IRecipient<DbImportedMessage>,
        IRecipient<HeatmapWeekStartsOnMondayChangedMessage>, IRecipient<HeatmapGitHubStyleChangedMessage>
    {
        readonly WorkoutService workoutService;
        readonly HeatmapSettingsService heatmapSettingsService;

        public MonthlyHeatmapService(WorkoutService workoutService, HeatmapSettingsService heatmapSettingsService)
        {
            this.workoutService = workoutService;
            this.heatmapSettingsService = heatmapSettingsService;
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        public void Receive(WorkoutFinishedMessage message)
        {
            var start = message.Value.StartTime;
            if (start is null) return;

            var startLocal = WorkoutService.ToLocal(start.Value);
            var (cachedYear, cachedMonth, days, weekStartsOnMonday, gitHubStyle) = ReadCache();

            if (cachedYear != startLocal.Year || cachedMonth != startLocal.Month)
                return;

            days.Add(startLocal.Day);
            WriteCache(cachedYear, cachedMonth, days, weekStartsOnMonday, gitHubStyle);
            RefreshWidget();
        }

        public async void Receive(DbImportedMessage message)
        {
            await RebuildCacheForCurrentMonthAsync();
            RefreshWidget();
        }

        public void Receive(HeatmapWeekStartsOnMondayChangedMessage message)
        {
            var (year, month, days, _, gitHubStyle) = ReadCache();
            WriteCache(year, month, days, message.Value, gitHubStyle);
            RefreshWidget();
        }

        public void Receive(HeatmapGitHubStyleChangedMessage message)
        {
            var (year, month, days, weekStartsOnMonday, _) = ReadCache();
            WriteCache(year, month, days, weekStartsOnMonday, message.Value);
            RefreshWidget();
        }

        public async Task RebuildCacheForCurrentMonthAsync()
        {
            var nowLocal = DateTime.Now;
            var days = await workoutService.GetWorkoutDaysInMonthAsync(nowLocal.Year, nowLocal.Month)
                       ?? new HashSet<int>();
            WriteCache(nowLocal.Year, nowLocal.Month, days,
                heatmapSettingsService.WeekStartsOnMonday, heatmapSettingsService.GitHubStyle);
        }

#if ANDROID
        public const string PrefsName = "FitISO.MonthlyHeatmapWidget";
        public const string CachedYearKey = "cached_year";
        public const string CachedMonthKey = "cached_month";
        public const string CachedDaysKey = "cached_workout_days";
        public const string CachedWeekStartsOnMondayKey = "cached_week_starts_on_monday";
        public const string CachedGitHubStyleKey = "cached_github_style";

        static (int year, int month, HashSet<int> days, bool weekStartsOnMonday, bool gitHubStyle) ReadCache()
        {
            var prefs = global::Android.App.Application.Context.GetSharedPreferences(PrefsName, FileCreationMode.Private);
            var year = prefs?.GetInt(CachedYearKey, 0) ?? 0;
            var month = prefs?.GetInt(CachedMonthKey, 0) ?? 0;
            var json = prefs?.GetString(CachedDaysKey, null);
            var weekStartsOnMonday = prefs?.GetBoolean(CachedWeekStartsOnMondayKey, true) ?? true;
            var gitHubStyle = prefs?.GetBoolean(CachedGitHubStyleKey, false) ?? false;

            HashSet<int> days;
            try
            {
                days = string.IsNullOrEmpty(json)
                    ? new HashSet<int>()
                    : JsonSerializer.Deserialize<HashSet<int>>(json) ?? new HashSet<int>();
            }
            catch
            {
                days = new HashSet<int>();
            }

            return (year, month, days, weekStartsOnMonday, gitHubStyle);
        }

        static void WriteCache(int year, int month, HashSet<int> days, bool weekStartsOnMonday, bool gitHubStyle)
        {
            var prefs = global::Android.App.Application.Context.GetSharedPreferences(PrefsName, FileCreationMode.Private);
            using var editor = prefs!.Edit();
            editor!.PutInt(CachedYearKey, year);
            editor!.PutInt(CachedMonthKey, month);
            editor!.PutString(CachedDaysKey, JsonSerializer.Serialize(days));
            editor!.PutBoolean(CachedWeekStartsOnMondayKey, weekStartsOnMonday);
            editor!.PutBoolean(CachedGitHubStyleKey, gitHubStyle);
            editor!.Apply();
        }

        static void RefreshWidget()
        {
            var context = global::Android.App.Application.Context;
            var intent = new Intent(context, typeof(MonthlyHeatmapWidgetProvider));
            intent.SetAction(MonthlyHeatmapWidgetProvider.ActionRefresh);
            context.SendBroadcast(intent);
        }
#else
        static (int year, int month, HashSet<int> days, bool weekStartsOnMonday, bool gitHubStyle) ReadCache()
            => (0, 0, new HashSet<int>(), true, false);
        static void WriteCache(int year, int month, HashSet<int> days, bool weekStartsOnMonday, bool gitHubStyle) { }
        static void RefreshWidget() { }
#endif
    }
}