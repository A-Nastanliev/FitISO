using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Maui.Models;
using FitISO.Maui.Views;
using FitISO.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FitISO.Maui.ViewModels
{
    public partial class WorkoutTemplatesViewModel : PagedCollectionViewModel<FitISO.Data.Models.Workout, Workout, int?>,
          IRecipient<WorkoutTemplateCreatedMessage>, IRecipient<ExerciseUpdatedMessage>, IRecipient<DbImportedMessage>
    {
        readonly WorkoutService workoutService;
        readonly WorkoutExerciseService workoutExerciseService;

        public WorkoutTemplatesViewModel(WorkoutService workoutService, WorkoutExerciseService workoutExerciseService)
        {
            this.workoutService = workoutService;
            this.workoutExerciseService = workoutExerciseService;
        }

        protected override int BatchSize => 8;

        protected override async Task<IReadOnlyList<FitISO.Data.Models.Workout>> FetchBatchAsync(int batchSize, int? cursor)
            => await workoutService.GetTemplatesAsync(batchSize, cursor);

        protected override Workout Wrap(FitISO.Data.Models.Workout raw) => new Workout(raw);

        protected override int? GetCursor(Workout item) => item.Id;

        [RelayCommand]
        public async Task AddWorkoutTemplate()
        {
            await Shell.Current.GoToAsync(nameof(WorkoutFormPage));
        }

        public void Receive(WorkoutTemplateCreatedMessage message)
        {
            Items.Add(message.Value);
            SyncCursorToTail();
        }

        [RelayCommand]
        public async Task DeleteWorkoutTemplate(Workout workout)
        {
            bool confirm = await Shell.Current.DisplayAlertAsync($"Delete {workout.Name}", $"Do you want to delete {workout.Name}?", "Yes", "No");

            if (!confirm)
                return;

            await workoutService.DeleteAsync(workout.Id);
            _ = Toast.Make($"{workout.Name} deleted").Show();
            Items.Remove(workout);
            SyncCursorToTail();
        }

        [RelayCommand]
        public async Task EditWorkoutTemplate(Workout workout)
        {
            await Shell.Current.GoToAsync(nameof(WorkoutFormPage), true,
                new Dictionary<string, object> { [nameof(WorkoutFormViewModel.NavigationWorkout)] = workout });
        }

        [RelayCommand]
        public async Task StartWorkout(Workout workout)
        {
            if (ActiveWorkoutState.Instance.HasActiveWorkout) return;

            Workout startWorkout = new Workout(await workoutService.StartFromTemplateAsync(workout.Id));
            WeakReferenceMessenger.Default.Send(new WorkoutStartedMessage(startWorkout));
            ActiveWorkoutState.Instance.HasActiveWorkout = true;
            await Shell.Current.GoToAsync("//active");
        }

        public async void Receive(DbImportedMessage message)
        {
            ResetPaging();
            await LoadFirst();
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