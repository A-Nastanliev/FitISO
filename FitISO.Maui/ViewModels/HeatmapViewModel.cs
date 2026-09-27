using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Maui.Models;
using FitISO.Maui.Services;
using FitISO.Services;

namespace FitISO.Maui.ViewModels
{
    public partial class HeatmapViewModel : ObservableObject, IRecipient<HeatmapWeekStartsOnMondayChangedMessage>,
        IRecipient<HeatmapGitHubStyleChangedMessage>, IRecipient<WorkoutFinishedMessage>, IRecipient<DbImportedMessage>
    {
        readonly WorkoutService workoutService;
        readonly HeatmapSettingsService heatmapSettingsService;
        int heatmapYear;
        int heatmapMonth;

        [ObservableProperty]
        HashSet<int> heatmapWorkoutDays = new();

        [ObservableProperty]
        int heatmapToday;

        [ObservableProperty]
        int heatmapDaysInMonth;

        [ObservableProperty]
        DayOfWeek heatmapFirstDayOfWeek;

        [ObservableProperty]
        DayOfWeek heatmapWeekStartDay;

        [ObservableProperty]
        bool heatmapUseGitHubStyleLayout;

        [ObservableProperty]
        string heatmapMonthLabel = string.Empty;

        [ObservableProperty]
        bool canGoPreviousMonth;

        [ObservableProperty]
        bool canGoNextMonth;

        [ObservableProperty]
        int heatmapMonthIndex;

        bool isFollowingCurrentMonth = true;

        readonly Dictionary<(int Year, int Month), HashSet<int>> monthDaysCache = new();
        readonly Queue<(int Year, int Month)> monthDaysCacheOrder = new();
        const int MonthDaysCacheLimit = 36;

        public HeatmapViewModel(WorkoutService workoutService, HeatmapSettingsService heatmapSettingsService)
        {
            this.workoutService = workoutService;
            this.heatmapSettingsService = heatmapSettingsService;

            WeakReferenceMessenger.Default.RegisterAll(this);

            heatmapWeekStartDay = heatmapSettingsService.WeekStartDay;
            heatmapUseGitHubStyleLayout = heatmapSettingsService.GitHubStyle;
        }

        public async Task LoadHeatmapAsync()
        {
            var now = DateTime.Now;
            var days = await workoutService.GetWorkoutDaysInMonthAsync(now.Year, now.Month) ?? new HashSet<int>();
            await ApplyHeatmapMonthAsync(now.Year, now.Month, days);
        }

        [RelayCommand]
        private async Task HeatmapPreviousMonthAsync()
        {
            var target = new DateTime(heatmapYear, heatmapMonth, 1).AddMonths(-1);

            if (!monthDaysCache.TryGetValue((target.Year, target.Month), out var days))
            {
                days = await workoutService.GetWorkoutDaysInMonthAsync(target.Year, target.Month);

                if (days is null)
                {
                    CanGoPreviousMonth = false;
                    return;
                }
            }

            await ApplyHeatmapMonthAsync(target.Year, target.Month, days);
        }

        [RelayCommand]
        private async Task HeatmapNextMonthAsync()
        {
            var now = DateTime.Now;
            var target = new DateTime(heatmapYear, heatmapMonth, 1).AddMonths(1);

            if (target.Year > now.Year || (target.Year == now.Year && target.Month > now.Month))
                return;

            if (!monthDaysCache.TryGetValue((target.Year, target.Month), out var days))
                days = await workoutService.GetWorkoutDaysInMonthAsync(target.Year, target.Month) ?? new HashSet<int>();

            await ApplyHeatmapMonthAsync(target.Year, target.Month, days);
        }

        [RelayCommand]
        private async Task HeatmapGoToFirstMonthAsync()
        {
            var earliest = await workoutService.GetEarliestWorkoutMonthAsync();
            if (earliest is null)
                return;

            var days = await workoutService.GetWorkoutDaysInMonthAsync(earliest.Value.Year, earliest.Value.Month)
                ?? new HashSet<int>();

            await ApplyHeatmapMonthAsync(earliest.Value.Year, earliest.Value.Month, days);
        }

        [RelayCommand]
        private Task HeatmapGoToCurrentMonthAsync() => LoadHeatmapAsync();

        async Task ApplyHeatmapMonthAsync(int year, int month, HashSet<int> days)
        {
            heatmapYear = year;
            heatmapMonth = month;

            var now = DateTime.Now;
            var isCurrentMonth = year == now.Year && month == now.Month;
            isFollowingCurrentMonth = isCurrentMonth;

            HeatmapWorkoutDays = days;
            HeatmapDaysInMonth = DateTime.DaysInMonth(year, month);
            HeatmapToday = isCurrentMonth ? now.Day : HeatmapDaysInMonth;
            HeatmapFirstDayOfWeek = new DateTime(year, month, 1).DayOfWeek;
            HeatmapMonthLabel = new DateTime(year, month, 1).ToString("MMMM yyyy");
            HeatmapMonthIndex = year * 12 + month;
            CanGoNextMonth = !isCurrentMonth;

            var previous = new DateTime(year, month, 1).AddMonths(-1);
            CanGoPreviousMonth = await workoutService.GetWorkoutDaysInMonthAsync(previous.Year, previous.Month) is not null;

            CacheMonthDays(year, month, days);
            _ = PrefetchAdjacentMonthsAsync(year, month);
        }

        void CacheMonthDays(int year, int month, HashSet<int> days)
        {
            var key = (year, month);

            if (!monthDaysCache.ContainsKey(key))
            {
                monthDaysCacheOrder.Enqueue(key);
                while (monthDaysCacheOrder.Count > MonthDaysCacheLimit)
                    monthDaysCache.Remove(monthDaysCacheOrder.Dequeue());
            }

            monthDaysCache[key] = days;
        }

        async Task PrefetchAdjacentMonthsAsync(int year, int month)
        {
            try
            {
                var now = DateTime.Now;
                var previous = new DateTime(year, month, 1).AddMonths(-1);
                var next = new DateTime(year, month, 1).AddMonths(1);
                var nextIsAllowed = next.Year < now.Year || (next.Year == now.Year && next.Month <= now.Month);

                if (!monthDaysCache.ContainsKey((previous.Year, previous.Month)))
                {
                    var previousDays = await workoutService.GetWorkoutDaysInMonthAsync(previous.Year, previous.Month);
                    if (previousDays is not null)
                        CacheMonthDays(previous.Year, previous.Month, previousDays);
                }

                if (nextIsAllowed && !monthDaysCache.ContainsKey((next.Year, next.Month)))
                {
                    var nextDays = await workoutService.GetWorkoutDaysInMonthAsync(next.Year, next.Month) ?? new HashSet<int>();
                    CacheMonthDays(next.Year, next.Month, nextDays);
                }
            }
            catch
            {
            }
        }

        public (int Year, int Month) CurrentYearMonth => (heatmapYear, heatmapMonth);

        public bool TryGetCachedDays(int year, int month, out HashSet<int> days)
            => monthDaysCache.TryGetValue((year, month), out days!);

        public Task RefreshHeatmapIfStaleAsync()
        {
            if (!isFollowingCurrentMonth)
                return Task.CompletedTask;

            var now = DateTime.Now;
            return (now.Year == heatmapYear && now.Month == heatmapMonth && now.Day == HeatmapToday)
                ? Task.CompletedTask : LoadHeatmapAsync();
        }

        public void Receive(HeatmapWeekStartsOnMondayChangedMessage message)
            => HeatmapWeekStartDay = message.Value ? DayOfWeek.Monday : DayOfWeek.Sunday;

        public void Receive(HeatmapGitHubStyleChangedMessage message)
            => HeatmapUseGitHubStyleLayout = message.Value;

        public void Receive(WorkoutFinishedMessage message)
        {
            var start = message.Value.StartTime;
            if (start is null)
                return;

            var startLocal = WorkoutService.ToLocal(start.Value);
            if (startLocal.Year != heatmapYear || startLocal.Month != heatmapMonth)
                return;

            var updatedDays = new HashSet<int>(HeatmapWorkoutDays) { startLocal.Day };
            HeatmapWorkoutDays = updatedDays;
            CacheMonthDays(heatmapYear, heatmapMonth, updatedDays);
        }

        public async void Receive(DbImportedMessage message) => await LoadHeatmapAsync();
    }
}