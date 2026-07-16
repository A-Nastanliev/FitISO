using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitISO.Maui.Models;
using FitISO.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace FitISO.Maui.ViewModels
{
    public partial class ExerciseCollection : ObservableObject
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
                    cursor = exercises[^1].Name;

                EndLoading(exercises.Count, cursor);
            }
            catch (Exception ex)
            {
                Loading = false;
                await Shell.Current.DisplayAlertAsync(ex.Message, ex.InnerException.ToString(), "OK");
            }
        }
    }
}
