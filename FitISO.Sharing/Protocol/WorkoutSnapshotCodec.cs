using System.Text.Json;
using FitISO.Sharing.Snapshots;

namespace FitISO.Sharing.Protocol
{
    public static class WorkoutSnapshotCodec
    {
        public const int CurrentProtocolVersion = 1;

        static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

        public static byte[] Encode(WorkoutSnapshot snapshot, DateTime sentAtUtc)
        {
            var envelope = new SnapshotEnvelope(CurrentProtocolVersion, sentAtUtc, snapshot);
            return JsonSerializer.SerializeToUtf8Bytes(envelope, Options);
        }

        public static bool TryDecode(byte[] payload, out SnapshotEnvelope envelope)
        {
            envelope = null;
            if (payload == null || payload.Length == 0)
                return false;

            try
            {
                var decoded = JsonSerializer.Deserialize<SnapshotEnvelope>(payload, Options);
                if (decoded == null
                    || decoded.ProtocolVersion != CurrentProtocolVersion
                    || !IsValid(decoded.Snapshot))
                    return false;

                envelope = decoded;
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        static bool IsValid(WorkoutSnapshot s) =>
            s != null
            && s.Name != null
            && s.Rest != null
            && s.Exercises != null
            && s.Exercises.All(e => e != null && e.Name != null && e.Sets != null && e.Sets.All(set => set != null));
    }
}