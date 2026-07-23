using CommunityToolkit.Mvvm.ComponentModel;
using FitISO.Maui.Models;

namespace FitISO.Maui.ViewModels
{
    public partial class SelectExercisePopupViewModel : ObservableObject
    {
        [ObservableProperty]
        ExerciseCollection exerciseCollection;

        [ObservableProperty]
        Exercise selectedExercise;

        readonly TaskCompletionSource<Exercise> resultSource = new();

        public Task<Exercise> Result => resultSource.Task;

        public SelectExercisePopupViewModel(ExerciseCollection exerciseCollection)
        {
            ExerciseCollection = exerciseCollection;
        }

        public async Task EnsureLoaded()
        {
          await  ExerciseCollection.Load();
        }

        partial void OnSelectedExerciseChanged(Exercise value)
        {
            if (value is null) return;

            resultSource.TrySetResult(value);
            _ = Shell.Current.Navigation.PopModalAsync();
        }

        public void CompleteIfNotAlready()
            => resultSource.TrySetResult(null);
    }
}
