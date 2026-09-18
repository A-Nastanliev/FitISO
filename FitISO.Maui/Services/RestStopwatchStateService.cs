namespace FitISO.Maui.Services
{
    public class RestStopwatchStateService
    {
        const string WorkoutIdKey = "ActiveRest_WorkoutId";
        const string StartTimeTicksKey = "ActiveRest_StartTimeTicks";
        const string IsStoppedKey = "ActiveRest_IsStopped";
        const string TriggerExerciseIdKey = "ActiveRest_TriggerExerciseId";
        const string TriggerSetIdKey = "ActiveRest_TriggerSetId";

        public int WorkoutId
        {
            get => Preferences.Default.Get(WorkoutIdKey, 0);
            set => Preferences.Default.Set(WorkoutIdKey, value);
        }

        public DateTime? StartTimeUtc
        {
            get
            {
                var ticks = Preferences.Default.Get(StartTimeTicksKey, 0L);
                return ticks > 0 ? new DateTime(ticks, DateTimeKind.Utc) : null;
            }
            set => Preferences.Default.Set(StartTimeTicksKey, value?.Ticks ?? 0L);
        }

        public bool IsStopped
        {
            get => Preferences.Default.Get(IsStoppedKey, false);
            set => Preferences.Default.Set(IsStoppedKey, value);
        }

        public int TriggerExerciseId
        {
            get => Preferences.Default.Get(TriggerExerciseIdKey, 0);
            set => Preferences.Default.Set(TriggerExerciseIdKey, value);
        }

        public int TriggerSetId
        {
            get => Preferences.Default.Get(TriggerSetIdKey, 0);
            set => Preferences.Default.Set(TriggerSetIdKey, value);
        }

        public void Clear()
        {
            Preferences.Default.Remove(WorkoutIdKey);
            Preferences.Default.Remove(StartTimeTicksKey);
            Preferences.Default.Remove(IsStoppedKey);
            Preferences.Default.Remove(TriggerExerciseIdKey);
            Preferences.Default.Remove(TriggerSetIdKey);
        }
    }
}