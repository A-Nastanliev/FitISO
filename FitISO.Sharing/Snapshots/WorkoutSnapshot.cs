namespace FitISO.Sharing.Snapshots
{
    public record WorkoutSnapshot( long Version, string Name, DateTime? StartTimeUtc, DateTime? EndTimeUtc, 
        IReadOnlyList<ExerciseSnapshot> Exercises, RestStopwatchSnapshot Rest);
}
