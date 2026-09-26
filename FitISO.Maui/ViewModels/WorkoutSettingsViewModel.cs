using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Maui.Services;

namespace FitISO.Maui.ViewModels
{
    public partial class WorkoutSettingsViewModel : ObservableObject, IRecipient<RestStopwatchEnabledChangedMessage>,
        IRecipient<ProgressCardEnabledChangedMessage>
    {
        [ObservableProperty]
        bool autoStartRestOnExerciseFinish;

        [ObservableProperty]
        bool restStopwatchEnabled;

        [ObservableProperty]
        bool progressCardEnabled;

        readonly WorkoutSettingsService workoutSettingsService;

        public WorkoutSettingsViewModel(WorkoutSettingsService workoutSettingsService)
        {
            this.workoutSettingsService = workoutSettingsService;

            WeakReferenceMessenger.Default.RegisterAll(this);

            AutoStartRestOnExerciseFinish = workoutSettingsService.AutoStartRestOnExerciseFinish;
            RestStopwatchEnabled = workoutSettingsService.RestStopwatchEnabled;
            ProgressCardEnabled = workoutSettingsService.ProgressCardEnabled;
        }

        partial void OnAutoStartRestOnExerciseFinishChanged(bool value) => workoutSettingsService.AutoStartRestOnExerciseFinish = value;

        partial void OnRestStopwatchEnabledChanged(bool value) => workoutSettingsService.RestStopwatchEnabled = value;

        partial void OnProgressCardEnabledChanged(bool value) => workoutSettingsService.ProgressCardEnabled = value;

        public void Receive(RestStopwatchEnabledChangedMessage message) => RestStopwatchEnabled = message.Value;

        public void Receive(ProgressCardEnabledChangedMessage message) => ProgressCardEnabled = message.Value;
    }
}
