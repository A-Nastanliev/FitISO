using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;

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
    }
}