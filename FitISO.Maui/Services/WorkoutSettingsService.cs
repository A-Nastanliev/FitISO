using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Maui.Models;

namespace FitISO.Maui.Services
{
    public class WorkoutSettingsService
    {
        const string AutoStartRestOnExerciseFinishKey = "AutoStartRestOnExerciseFinish";

        public bool AutoStartRestOnExerciseFinish
        {
            get => Preferences.Default.Get(AutoStartRestOnExerciseFinishKey, false);
            set => Preferences.Default.Set(AutoStartRestOnExerciseFinishKey, value);
        }

        const string RestStopwatchEnabledKey = "RestStopwatchEnabled";

        public bool RestStopwatchEnabled
        {
            get => Preferences.Default.Get(RestStopwatchEnabledKey, true);
            set
            {
                if (RestStopwatchEnabled == value) return;
                Preferences.Default.Set(RestStopwatchEnabledKey, value);
                WeakReferenceMessenger.Default.Send(new RestStopwatchEnabledChangedMessage(value));
            }
        }

        const string ProgressCardEnabledKey = "ProgressCardEnabled";

        public bool ProgressCardEnabled
        {
            get => Preferences.Default.Get(ProgressCardEnabledKey, true);
            set
            {
                if (ProgressCardEnabled == value) return;
                Preferences.Default.Set(ProgressCardEnabledKey, value);
                WeakReferenceMessenger.Default.Send(new ProgressCardEnabledChangedMessage(value));
            }
        }

        const string WorkoutLayoutKey = "WorkoutLayout";

        public WorkoutLayout WorkoutLayout
        {
            get
            {
                int stored = Preferences.Default.Get(WorkoutLayoutKey, (int)WorkoutLayout.Classic);
                return Enum.IsDefined((WorkoutLayout)stored) ? (WorkoutLayout)stored : WorkoutLayout.Classic;
            }
            set
            {
                if (WorkoutLayout == value) return;
                Preferences.Default.Set(WorkoutLayoutKey, (int)value);
                WeakReferenceMessenger.Default.Send(new WorkoutLayoutChangedMessage(value));
            }
        }
    }
}