using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Maui.Models;
using FitISO.Maui.Services;
using FitISO.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FitISO.Maui.ViewModels
{
    public partial class HistoryPageViewModel : PagedCollectionViewModel<FitISO.Data.Models.Workout, Workout, int?>,
          IRecipient<WorkoutFinishedMessage>, IRecipient<ExerciseUpdatedMessage>, IRecipient<DbImportedMessage>
    {
        readonly WorkoutService workoutService;
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
        string heatmapMonthLabel = string.Empty;

        [ObservableProperty]
        bool canGoPreviousMonth;

        [ObservableProperty]
        bool canGoNextMonth;

        bool isFollowingCurrentMonth = true;

        public HistoryPageViewModel(WorkoutService workoutService)
        {
            this.workoutService = workoutService;
        }

        protected override int BatchSize => 6;

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
            var days = await workoutService.GetWorkoutDaysInMonthAsync(target.Year, target.Month);

            if (days is null)
            {
                CanGoPreviousMonth = false;
                return;
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

            var days = await workoutService.GetWorkoutDaysInMonthAsync(target.Year, target.Month) ?? new HashSet<int>();
            await ApplyHeatmapMonthAsync(target.Year, target.Month, days);
        }

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

            CanGoNextMonth = !isCurrentMonth;

            var previous = new DateTime(year, month, 1).AddMonths(-1);
            CanGoPreviousMonth = await workoutService.GetWorkoutDaysInMonthAsync(previous.Year, previous.Month) is not null;
        }
        public Task RefreshHeatmapIfStaleAsync()
        {
            if (!isFollowingCurrentMonth)
                return Task.CompletedTask;

            var now = DateTime.Now;
            return (now.Year == heatmapYear && now.Month == heatmapMonth && now.Day == HeatmapToday)
                ? Task.CompletedTask : LoadHeatmapAsync();
        }

        protected override async Task<IReadOnlyList<FitISO.Data.Models.Workout>> FetchBatchAsync(int batchSize, int? cursor)
            => await workoutService.GetWorkoutsAsync(batchSize, cursor);

        protected override Workout Wrap(FitISO.Data.Models.Workout raw) => new Workout(raw);

        protected override int? GetCursor(Workout item) => item.Id;

        [RelayCommand]
        private async Task ExportWorkoutImageAsync(Workout workout)
        {
            if (workout is null)
                return;

            try
            {
                using var stream = new MemoryStream();
                WorkoutImageBuilder.Build(workout, stream);
                stream.Position = 0;

                var invalidChars = Path.GetInvalidFileNameChars();
                var safeWorkoutName = string.Concat(workout.Name.Split(invalidChars));
                var fileName = $"FitISO_{safeWorkoutName}_{workout.StartTime:yyyy_MM_dd}.png";

                var result = await FileSaver.Default.SaveAsync(fileName, stream, CancellationToken.None);

                if (result.IsSuccessful)
                {
                    _ = Toast.Make("Image saved").Show();
                }
                else if (result.Exception is not null && result.Exception is not OperationCanceledException)
                {
                    await Shell.Current.DisplayAlertAsync("Export failed", result.Exception.Message, "OK");
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Export failed", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private async Task ExportWorkoutPdfAsync(Workout workout)
        {
            if (workout is null)
                return;

            try
            {
                using var stream = new MemoryStream();
                WorkoutPdfBuilder.Build(workout, stream);
                stream.Position = 0;

                var invalidChars = Path.GetInvalidFileNameChars();
                var safeWorkoutName = string.Concat(workout.Name.Split(invalidChars));
                var fileName = $"FitISO_{safeWorkoutName}_{workout.StartTime:yyyy_MM_dd}.pdf";

                var result = await FileSaver.Default.SaveAsync(fileName, stream, CancellationToken.None);

                if (result.IsSuccessful)
                {
                    _ = Toast.Make("PDF saved").Show();
                }
                else if (result.Exception is not null && result.Exception is not OperationCanceledException)
                {
                    await Shell.Current.DisplayAlertAsync("Export failed", result.Exception.Message, "OK");
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Export failed", ex.Message, "OK");
            }
        }

        [RelayCommand]
        public async Task StartWorkout(Workout workout)
        {
            if (ActiveWorkoutState.Instance.HasActiveWorkout) return;

            try
            {
                Workout startWorkout = new Workout(await workoutService.StartFromWorkoutAsync(workout.Id));
                WeakReferenceMessenger.Default.Send(new WorkoutStartedMessage(startWorkout));
                ActiveWorkoutState.Instance.HasActiveWorkout = true;
                await Shell.Current.GoToAsync("//active");
            }
            catch(Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private async Task SaveAsTemplateAsync(Workout workout)
        {
            try
            {
                var newTemplate = new Workout(await workoutService.TemplateFromWorkoutAsync(workout.Id));
                WeakReferenceMessenger.Default.Send(new WorkoutTemplateCreatedMessage(newTemplate));
                _ = Toast.Make($"{workout.Name} saved as template").Show();
                await Shell.Current.GoToAsync("//workouts");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Save failed", ex.Message, "OK");
            }
        }

        public async void Receive(DbImportedMessage message)
        {
            ResetPaging();
            await LoadFirst();
            await LoadHeatmapAsync();
        }

        public void Receive(WorkoutFinishedMessage message)
        {
            Items.Insert(0, message.Value);

            var start = message.Value.StartTime;
            if (start is null)
                return;

            var startLocal = WorkoutService.ToLocal(start.Value);
            if (startLocal.Year != heatmapYear || startLocal.Month != heatmapMonth)
                return;

            var updatedDays = new HashSet<int>(HeatmapWorkoutDays) { startLocal.Day };
            HeatmapWorkoutDays = updatedDays;
        }

        public void Receive(ExerciseUpdatedMessage message)
        {
            Exercise exercise = message.Value;
            foreach (var w in Items)
            {
                foreach (var we in w.WorkoutExercises)
                {
                    if (we.Exercise.Id == exercise.Id)
                    {
                        we.Exercise.Name = exercise.Name;
                    }
                }
            }
        }
    }
}