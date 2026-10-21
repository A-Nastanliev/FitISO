namespace FitISO.Sharing.Snapshots
{
    public record ExerciseSnapshot(string Name, IReadOnlyList<SetSnapshot> Sets);
}
