using FitISO.Sharing.Snapshots;

namespace FitISO.Sharing.Protocol
{
    public record SnapshotEnvelope(int ProtocolVersion, DateTime SentAtUtc, WorkoutSnapshot Snapshot);
}