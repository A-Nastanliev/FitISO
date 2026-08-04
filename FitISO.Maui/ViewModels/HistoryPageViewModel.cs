using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Maui.Models;
using FitISO.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace FitISO.Maui.ViewModels
{
    public partial class HistoryPageViewModel : ObservableObject, IRecipient<WorkoutFinishedMessage>, IRecipient<DbImportedMessage>, IRecipient<ExerciseUpdatedMessage>
    {
        [ObservableProperty]
        ObservableCollection<Workout> workouts = new();
        [ObservableProperty]
        bool loading;
        const int batchSize = 20;
        int? cursor;
        bool canLoadMore = true;
        readonly WorkoutService workoutService;

        public HistoryPageViewModel(WorkoutService workoutService)
        {
            WeakReferenceMessenger.Default.RegisterAll(this);
            this.workoutService = workoutService;
        }

        private bool CanStartLoading()
        => !Loading && canLoadMore;

        private void BeginLoading()
            => Loading = true;

        private void EndLoading(int itemsLoaded)
        {
            Loading = false;

            if (itemsLoaded < batchSize)
                canLoadMore = false;
        }

        [RelayCommand]
        public async Task Load()
        {
            if (!CanStartLoading()) return;

            BeginLoading();

            try
            {
                var workouts = await workoutService.GetWorkoutsAsync(batchSize, cursor);
                foreach (var workout in workouts)
                    Workouts.Add(new Workout(workout));

                if (workouts.Count > 0)
                    cursor = Workouts[^1].Id;

                EndLoading(workouts.Count);
            }
            catch (Exception ex)
            {
                Loading = false;
                await Shell.Current.DisplayAlertAsync(ex.Message, ex.InnerException.ToString(), "OK");
            }
        }

        public void Receive(WorkoutFinishedMessage message)
        {
            Workouts.Insert(0, message.Value);
        }

        public async void Receive(DbImportedMessage message)
        {
            Workouts.Clear();
            cursor = null;
            canLoadMore = true;
            await Load();
        }

        public void Receive(ExerciseUpdatedMessage message)
        {
            Exercise exercise = message.Value;
            foreach (var w in Workouts)
            {
                foreach(var we in w.WorkoutExercises)
                {
                    if(we.Exercise.Id == exercise.Id)
                    {
                        we.Exercise.Name = exercise.Name;
                    }
                }
            }
        }
    }
}
