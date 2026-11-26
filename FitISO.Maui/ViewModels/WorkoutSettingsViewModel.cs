using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Maui.Models;
using FitISO.Maui.Services;
using System.Collections.ObjectModel;

namespace FitISO.Maui.ViewModels
{
    public partial class WorkoutSettingsViewModel : ObservableObject, IRecipient<RestStopwatchEnabledChangedMessage>,
        IRecipient<ProgressCardEnabledChangedMessage>, IRecipient<WorkoutLayoutChangedMessage>
    {
        public ObservableCollection<WorkoutLayoutOption> LayoutOptions { get; } = new()
        {
            new WorkoutLayoutOption(WorkoutLayout.Classic, "Classic"),
            new WorkoutLayoutOption(WorkoutLayout.Ring, "Ring"),
            new WorkoutLayoutOption(WorkoutLayout.CompactBar, "Compact bar")
        };

        [ObservableProperty]
        WorkoutLayoutOption selectedLayoutOption;

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

            selectedLayoutOption = LayoutOptions.FirstOrDefault(o => o.Layout == workoutSettingsService.WorkoutLayout) ?? LayoutOptions[0];
        }

        partial void OnAutoStartRestOnExerciseFinishChanged(bool value) => workoutSettingsService.AutoStartRestOnExerciseFinish = value;

        partial void OnRestStopwatchEnabledChanged(bool value) => workoutSettingsService.RestStopwatchEnabled = value;

        partial void OnProgressCardEnabledChanged(bool value) => workoutSettingsService.ProgressCardEnabled = value;

        partial void OnSelectedLayoutOptionChanged(WorkoutLayoutOption value)
        {
            if (value is null) return;

            workoutSettingsService.WorkoutLayout = value.Layout;
        }

        public void Receive(RestStopwatchEnabledChangedMessage message) => RestStopwatchEnabled = message.Value;

        public void Receive(ProgressCardEnabledChangedMessage message) => ProgressCardEnabled = message.Value;

        public void Receive(WorkoutLayoutChangedMessage message)
        {
            var option = LayoutOptions.FirstOrDefault(o => o.Layout == message.Value);
            if (option is not null)
                SelectedLayoutOption = option;
        }
    }
}