using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Maui.Models;
using FitISO.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitISO.Maui.ViewModels
{
    public partial class ExerciseCollection : PagedCollectionViewModel<FitISO.Data.Models.Exercise, Exercise, string>,
        IRecipient<WorkoutFinishedMessage>, IRecipient<DbImportedMessage>
    {
        readonly ExerciseService exerciseService;

        public ExerciseCollection(ExerciseService exerciseService)
        {
            this.exerciseService = exerciseService;
        }

        protected override int BatchSize => 20;

        protected override async Task<IReadOnlyList<FitISO.Data.Models.Exercise>> FetchBatchAsync(int batchSize, string cursor)
            => await exerciseService.GetNextAsync(batchSize, cursor);

        protected override Exercise Wrap(FitISO.Data.Models.Exercise raw) => new Exercise(raw);

        protected override string GetCursor(Exercise item) => item.Name;

        public void Add(Exercise exercise)
        {
            var index = 0;
            while (index < Items.Count &&
                   string.Compare(Items[index].Name, exercise.Name, StringComparison.OrdinalIgnoreCase) < 0)
                index++;

            Items.Insert(index, exercise);
            SyncCursorToTail();
        }

        public void Reposition(Exercise exercise)
        {
            var currentIndex = Items.IndexOf(exercise);
            if (currentIndex < 0) return;

            var index = 0;
            for (var i = 0; i < Items.Count; i++)
            {
                if (i == currentIndex) continue;

                if (string.Compare(Items[i].Name, exercise.Name, StringComparison.OrdinalIgnoreCase) < 0)
                    index++;
            }

            if (index != currentIndex)
                Items.Move(currentIndex, index);
        }

        public void Remove(Exercise exercise)
        {
            Items.Remove(exercise);
            SyncCursorToTail();
        }

        public void Receive(WorkoutFinishedMessage message)
        {
            foreach (var workoutExercise in message.Value.WorkoutExercises)
            {
                var exercise = Items.FirstOrDefault(e => e.Id == workoutExercise.Exercise.Id);
                if (exercise is not null)
                {
                    exercise.LastSets = workoutExercise.Sets;
                    exercise.LastSetsDate = message.Value.StartTime;
                    if (exercise.BestSet is null || exercise.BestSet.Weight is 0 || exercise.BestSet.Weight is null)
                        exercise.BestSet = workoutExercise.Sets[0];

                    Set historyPoint = workoutExercise.Sets[0];
                    for (int i = 1; i < workoutExercise.Sets.Count; i++)
                    {
                        if (IsBetterSet(workoutExercise.Sets[i], historyPoint))
                        {
                            historyPoint = workoutExercise.Sets[i];
                        }
                    }

                    exercise.History.Add(new ExerciseHistoryPoint(exercise.LastSetsDate.Value, historyPoint.Weight.Value, historyPoint.Reps.Value));

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

        public async void Receive(DbImportedMessage message)
        {
            ResetPaging();
            await LoadFirst();
        }
    }
}