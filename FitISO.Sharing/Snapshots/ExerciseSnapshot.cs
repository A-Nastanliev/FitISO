using System.Text.Json.Serialization;

namespace FitISO.Sharing.Snapshots
{
    public record ExerciseSnapshot(string Name, IReadOnlyList<SetSnapshot> Sets, 
        SetSnapshot? BestSet = null, IReadOnlyList<SetSnapshot>? LastSets = null, DateTime? LastSetsDateUtc = null)
    {
        [JsonIgnore] public bool HasBestSet => BestSet is not null;
        [JsonIgnore] public bool HasLastSets => LastSets is { Count: > 0 };
        [JsonIgnore] public bool HasHistory => HasBestSet || HasLastSets;
    }
}
