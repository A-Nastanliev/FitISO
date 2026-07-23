using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Maui.Models;
using FitISO.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace FitISO.Maui.ViewModels
{
    public partial class ExerciseCollection : ObservableObject, IRecipient<WorkoutFinishedMessage>
    {
        [ObservableProperty]
        ObservableCollection<Exercise> exercises = new();
        [ObservableProperty]
        bool loading;
        const int batchSize = 20;
        string cursor;
        bool canLoadMore = true;
        readonly ExerciseService exerciseService;

        public ExerciseCollection(ExerciseService exerciseService)
        {
            this.exerciseService = exerciseService;
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        private bool CanStartLoading()
            => !Loading && canLoadMore;

        private void BeginLoading()
            => Loading = true;

        private void EndLoading(int itemsLoaded, string cursor )
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
                var exercises = await exerciseService.GetNextAsync(batchSize, cursor);
                foreach (var exercise in exercises)
                    Exercises.Add(new Exercise(exercise));

                if (exercises.Count > 0)
                    cursor = Exercises[^1].Name;

                EndLoading(exercises.Count, cursor);
            }
            catch (Exception ex)
            {
                Loading = false;
                await Shell.Current.DisplayAlertAsync(ex.Message, ex.InnerException.ToString(), "OK");
            }
        }

        public void Add(Exercise exercise)
        {
            var index = 0;
            while (index < Exercises.Count &&
                   string.Compare(Exercises[index].Name, exercise.Name, StringComparison.OrdinalIgnoreCase) < 0)
                index++;

            Exercises.Insert(index, exercise);
            cursor = Exercises[^1].Name;
        }

        public void Reposition(Exercise exercise)
        {
            var currentIndex = Exercises.IndexOf(exercise);
            if (currentIndex < 0) return;

            var index = 0;
            for (var i = 0; i < Exercises.Count; i++)
            {
                if (i == currentIndex) continue;

                if (string.Compare(Exercises[i].Name, exercise.Name, StringComparison.OrdinalIgnoreCase) < 0)
                    index++;
            }

            if (index != currentIndex)
                Exercises.Move(currentIndex, index);
        }

        public void Remove(Exercise exercise) => Exercises.Remove(exercise);

        public void Receive(WorkoutFinishedMessage message)
        {
            foreach(var workoutExercise in message.Value.WorkoutExercises)
            {
                var exercise = Exercises.FirstOrDefault(e => e.Id == workoutExercise.Exercise.Id);
                if(exercise is not null)
                {
                    exercise.LastSets = workoutExercise.Sets;
                    if (exercise.BestSet is null || exercise.BestSet.Weight is 0 || exercise.BestSet.Weight is null)
                        exercise.BestSet = workoutExercise.Sets[0];

                    foreach (var set in workoutExercise.Sets)
                    {
                        if (IsBetterSet(set, exercise.BestSet))
                        {
                            exercise.BestSet = set;
                        }
                    }
                }
            }
        }

        private static bool IsBetterSet(Set candidate, Set? currentBest)
        {
            if (candidate.Weight > currentBest.Weight)
                return true;

            if (candidate.Weight == currentBest.Weight && candidate.Reps > currentBest.Reps)
                return true;

            return false;
        }
    }
}
