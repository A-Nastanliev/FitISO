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
    public class MonthlyHeatmapService : IRecipient<WorkoutFinishedMessage>, IRecipient<DbImportedMessage>
    {
        readonly WorkoutService workoutService;

        public MonthlyHeatmapService(WorkoutService workoutService)
        {
            this.workoutService = workoutService;
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        public void Receive(WorkoutFinishedMessage message)
        {
            var start = message.Value.StartTime;
            if (start is null) return;

            var startLocal = WorkoutService.ToLocal(start.Value);
            var (cachedYear, cachedMonth, days) = ReadCache();

            if (cachedYear != startLocal.Year || cachedMonth != startLocal.Month)
                return;

            days.Add(startLocal.Day);
            WriteCache(cachedYear, cachedMonth, days);
            RefreshWidget();
        }

        public async void Receive(DbImportedMessage message)
        {
            await RebuildCacheForCurrentMonthAsync();
            RefreshWidget();
        }

        public async Task RebuildCacheForCurrentMonthAsync()
        {
            var nowLocal = DateTime.Now;
            var days = await workoutService.GetWorkoutDaysInMonthAsync(nowLocal.Year, nowLocal.Month)
                       ?? new HashSet<int>();
            WriteCache(nowLocal.Year, nowLocal.Month, days);
        }

#if ANDROID
        public const string PrefsName = "FitISO.MonthlyHeatmapWidget";
        public const string CachedYearKey = "cached_year";
        public const string CachedMonthKey = "cached_month";
        public const string CachedDaysKey = "cached_workout_days";

        static (int year, int month, HashSet<int> days) ReadCache()
        {
            var prefs = global::Android.App.Application.Context.GetSharedPreferences(PrefsName, FileCreationMode.Private);
            var year = prefs?.GetInt(CachedYearKey, 0) ?? 0;
            var month = prefs?.GetInt(CachedMonthKey, 0) ?? 0;
            var json = prefs?.GetString(CachedDaysKey, null);

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

            return (year, month, days);
        }

        static void WriteCache(int year, int month, HashSet<int> days)
        {
            var prefs = global::Android.App.Application.Context.GetSharedPreferences(PrefsName, FileCreationMode.Private);
            using var editor = prefs!.Edit();
            editor!.PutInt(CachedYearKey, year);
            editor!.PutInt(CachedMonthKey, month);
            editor!.PutString(CachedDaysKey, JsonSerializer.Serialize(days));
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
        static (int year, int month, HashSet<int> days) ReadCache() => (0, 0, new HashSet<int>());
        static void WriteCache(int year, int month, HashSet<int> days) { }
        static void RefreshWidget() { }
#endif
    }
}