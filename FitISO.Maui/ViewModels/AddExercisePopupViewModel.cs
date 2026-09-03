using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitISO.Services;

namespace FitISO.Maui.ViewModels
{
    public partial class AddExercisePopupViewModel : ObservableObject
    {
        [ObservableProperty]
        string name;

        [ObservableProperty]
        bool busy;

        readonly TaskCompletionSource<Models.Exercise> resultSource = new();
        readonly ExerciseService exerciseService;

        public Task<Models.Exercise> Result => resultSource.Task;

        public AddExercisePopupViewModel(ExerciseService exerciseService)
        {
            this.exerciseService = exerciseService;
        }

        [RelayCommand]
        async Task Add()
        {
            if (string.IsNullOrWhiteSpace(Name) || Name?.Length < 4 || Busy)
                return;

            Busy = true;

            try
            {
                var exercise = await exerciseService.CreateAsync(Name);
                resultSource.TrySetResult(new Models.Exercise(exercise));
                await Shell.Current.Navigation.PopModalAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Exercise already exists", $"{Name} already exists. Try a different name.", "OK");
            }
            finally
            {
                Busy = false;
            }
        }

        [RelayCommand]
        async Task Cancel()
        {
            resultSource.TrySetResult(null);
            await Shell.Current.Navigation.PopModalAsync();
        }

        public void CompleteIfNotAlready()
            => resultSource.TrySetResult(null);
    }
}