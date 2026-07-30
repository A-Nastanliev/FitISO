using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Maui.Models;
using FitISO.Maui.Views;
using FitISO.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace FitISO.Maui.ViewModels
{
    public partial class WorkoutTemplatesViewModel : ObservableObject, IRecipient<WorkoutTemplateCreatedMessage>, IRecipient<DbImportedMessage>
    {
        [ObservableProperty]
        ObservableCollection<Workout> workoutTemplates = new();

        [ObservableProperty]
        bool loading;
        const int batchSize = 20;
        int? cursor;
        bool canLoadMore = true;

        readonly WorkoutService workoutService;
        readonly WorkoutExerciseService workoutExerciseService;

        public WorkoutTemplatesViewModel(WorkoutService workoutService, WorkoutExerciseService workoutExerciseService) 
        {
            this.workoutService = workoutService;
            this.workoutExerciseService = workoutExerciseService;
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        private bool CanStartLoading()
        => !Loading && canLoadMore;

        private void BeginLoading()
            => Loading = true;

        private void EndLoading(int itemsLoaded, int? cursor)
        {
            Loading = false;

            if (itemsLoaded < batchSize)
                canLoadMore = false;

            this.cursor = cursor;

        }

        [RelayCommand]
        public async Task Load()
        {
            if (!CanStartLoading()) return;

            BeginLoading();

            try
            {
                var workoutTemplates = await workoutService.GetTemplatesAsync(batchSize, cursor);
                foreach (FitISO.Data.Models.Workout workout in workoutTemplates)
                    WorkoutTemplates.Add(new Workout(workout));

                if (WorkoutTemplates.Count > 0)
                    cursor = WorkoutTemplates[^1].Id;

                EndLoading(workoutTemplates.Count, cursor);
            }
            catch (Exception ex)
            {
                Loading = false;
                await Shell.Current.DisplayAlertAsync(ex.Message, ex.InnerException.ToString(), "OK");
            }
        }

        [RelayCommand]
        public async Task AddWorkoutTemplate()
        {
            await Shell.Current.GoToAsync(nameof(WorkoutFormPage));
        }

        public void Receive(WorkoutTemplateCreatedMessage message)
        {
            WorkoutTemplates.Add(message.Value);
            cursor = WorkoutTemplates[^1].Id;
        }

        [RelayCommand]
        public async Task DeleteWorkoutTemplate(Workout workout)
        {
            bool confirm = await Shell.Current.DisplayAlertAsync($"Delete {workout.Name}", $"Do you want to delete {workout.Name}?", "Yes", "No");

            if (!confirm)
                return;

            await workoutService.DeleteAsync(workout.Id);
            _ = Toast.Make($"{workout.Name} deleted").Show();
            WorkoutTemplates.Remove(workout);
            cursor = WorkoutTemplates[^1].Id;
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

            Workout startWorkout =new Workout(await workoutService.StartFromTemplateAsync(workout.Id));
            WeakReferenceMessenger.Default.Send(new WorkoutStartedMessage(startWorkout));
            ActiveWorkoutState.Instance.HasActiveWorkout = true;
            await Shell.Current.GoToAsync("//active");
        }

        public async void Receive(DbImportedMessage message)
        {
            WorkoutTemplates.Clear();
            cursor = null;
            canLoadMore = true;       
            await Load();
        }
    }
}
