using FitISO.Sharing.Snapshots;

namespace FitISO.Sharing.Protocol
{
    public record SnapshotEnvelope(int ProtocolVersion, DateTime SentAtUtc, WorkoutSnapshot Snapshot, 
       MessageType Type = MessageType.Snapshot, long EndedVersion = 0);
}