using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitISO.Maui.Models;
using FitISO.Maui.Views;
using FitISO.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace FitISO.Maui.ViewModels
{
    public partial class ExercisePageViewModel : ObservableObject 
    {
        [ObservableProperty]
        ExerciseCollection exerciseCollection;

        [ObservableProperty]
        Exercise selectedExercise;

        readonly ExerciseService exerciseService;
        readonly IServiceProvider serviceProvider;

        public ExercisePageViewModel(ExerciseCollection exerciseCollection, ExerciseService exerciseService, IServiceProvider serviceProvider)
        {
            ExerciseCollection = exerciseCollection;
            this.exerciseService = exerciseService;
            this.serviceProvider = serviceProvider;
        }

        [RelayCommand]
        async Task AddExercise()
        {
            var popup = serviceProvider.GetRequiredService<AddExercisePopupPage>();
            var viewModel = (AddExercisePopupViewModel)popup.BindingContext;

            await Shell.Current.Navigation.PushModalAsync(popup);

            var exercise = await viewModel.Result;

            if (exercise is not null)
                ExerciseCollection.Add(exercise);
        }

        partial void OnSelectedExerciseChanged(Exercise value)
        {
            if (value is not null)
                _ = ShowExerciseDetails(value);
        }

        async Task ShowExerciseDetails(Exercise exercise)
        {
            var popup = serviceProvider.GetRequiredService<ExerciseDetailsPopupPage>();
            var viewModel = (ExerciseDetailsPopupViewModel)popup.BindingContext;
            viewModel.SetExercise(exercise);

            await Shell.Current.Navigation.PushModalAsync(popup);
            await viewModel.Closed;

            if (viewModel.WasDeleted)
                ExerciseCollection.Remove(exercise);
            else if (viewModel.WasRenamed)
                ExerciseCollection.Reposition(exercise);

            SelectedExercise = null;
        }
    }
}
