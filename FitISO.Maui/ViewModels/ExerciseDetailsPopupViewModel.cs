using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitISO.Maui.Models;
using FitISO.Services;

namespace FitISO.Maui.ViewModels
{
    public partial class ExerciseDetailsPopupViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowDeleteButton))]
        Exercise exercise = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        bool busy;

        public bool IsNotBusy => !Busy;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotEditingName))]
        [NotifyPropertyChangedFor(nameof(ShowDeleteButton))]
        bool isEditingName;

        public bool IsNotEditingName => !IsEditingName;

        string nameBeforeEdit;

        public bool ShowDeleteButton => Deletable && IsNotEditingName;

        public bool Deletable;

        public bool WasDeleted { get; private set; }
        public bool WasRenamed { get; private set; }

        readonly TaskCompletionSource closedSource = new();
        readonly ExerciseService exerciseService;

        public Task Closed => closedSource.Task;

        public ExerciseDetailsPopupViewModel(ExerciseService exerciseService)
        {
            this.exerciseService = exerciseService;
        }

        public async Task CheckIfDeletable()
        {
            Deletable = await exerciseService.IsDeletable(Exercise.Id);
            OnPropertyChanged(nameof(ShowDeleteButton));
        }

        public void SetExercise(Exercise exercise)
        {
            Exercise = exercise;
            IsEditingName = false;
            WasRenamed = false;
        }

        [RelayCommand]
        async Task Delete()
        {
            if (Busy) return;

            Busy = true;

            try
            {
                await exerciseService.DeleteAsync(Exercise.Id); 
                WasDeleted = true;
                closedSource.TrySetResult();
                await Shell.Current.Navigation.PopModalAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Couldn't delete", $"{Exercise.Name} couldn't be deleted. Try again.", "OK");
            }
            finally
            {
                Busy = false;
            }
        }

        public void CompleteIfNotAlready()
            => closedSource.TrySetResult();

        [RelayCommand]
        void StartRename()
        {
            nameBeforeEdit = Exercise.Name;
            IsEditingName = true;
        }

        [RelayCommand]
        void CancelRename()
        {
            Exercise.Name = nameBeforeEdit;
            IsEditingName = false;
        }

        [RelayCommand]
        async Task SaveRename()
        {
            if (Busy) return;

            if (string.IsNullOrWhiteSpace(Exercise.Name))
            {
                Exercise.Name = nameBeforeEdit;
                IsEditingName = false;
                return;
            }

            Busy = true;

            try
            {
                await exerciseService.UpdateNameAsync(Exercise.Id, Exercise.Name.Trim());
                WasRenamed = true;
                IsEditingName = false;
            }
            catch (Exception ex)
            {
                Exercise.Name = nameBeforeEdit;
                await Shell.Current.DisplayAlertAsync("Couldn't rename", $"The exercise couldn't be renamed. Try again.", "OK");
            }
            finally
            {
                Busy = false;
            }
        }
    }
}