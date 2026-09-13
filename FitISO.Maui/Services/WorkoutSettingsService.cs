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
    }
}