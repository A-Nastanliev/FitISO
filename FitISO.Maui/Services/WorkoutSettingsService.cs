namespace FitISO.Maui.Services
{
    public class WorkoutSettingsService
    {
        public const string AutoStartRestOnExerciseFinishKey = "AutoStartRestOnExerciseFinish";

        public bool AutoStartRestOnExerciseFinish
        {
            get => Preferences.Default.Get(AutoStartRestOnExerciseFinishKey, false);
            set => Preferences.Default.Set(AutoStartRestOnExerciseFinishKey, value);
        }

        public const string RestStopwatchEnabledKey = "RestStopwatchEnabled";

        public bool RestStopwatchEnabled
        {
            get => Preferences.Default.Get(RestStopwatchEnabledKey, true);
            set => Preferences.Default.Set(RestStopwatchEnabledKey, value);
        }
    }
}