using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
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

        public HistoryPageViewModel(WorkoutService workoutService)
        {
            this.workoutService = workoutService;
        }

        protected override int BatchSize => 6;

        protected override async Task<IReadOnlyList<FitISO.Data.Models.Workout>> FetchBatchAsync(int batchSize, int? cursor)
            => await workoutService.GetWorkoutsAsync(batchSize, cursor);

        protected override Workout Wrap(FitISO.Data.Models.Workout raw) => new Workout(raw);

        protected override int? GetCursor(Workout item) => item.Id;

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

        public async void Receive(DbImportedMessage message)
        {
            ResetPaging();
            await LoadFirst();
        }

        public void Receive(WorkoutFinishedMessage message)
        {
            Items.Insert(0, message.Value);
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