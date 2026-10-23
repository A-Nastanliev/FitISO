using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using FitISO.Sharing.Protocol;
using FitISO.Sharing.Snapshots;
using NUnit.Framework;

namespace FitISO.Tests.Sharing
{
    [TestFixture]
    public class WorkoutSnapshotCodecTests
    {
        private static readonly DateTime SentAt = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        private static WorkoutSnapshot CreateSnapshot(long version) => new(
            version,
            "Push day",
            SentAt,
            null,
            new List<ExerciseSnapshot>
            {
                new("Bench Press", new List<SetSnapshot>
                {
                    new(80, 8, true),
                    new(null, null, false)
                })
            },
            new RestStopwatchSnapshot(SentAt, false));

        private static string ToJson(object value) => JsonSerializer.Serialize(value);

        [Test]
        public void Encode_ThenTryDecode_RoundTripsSnapshot()
        {
            var original = CreateSnapshot(5);

            var decoded = WorkoutSnapshotCodec.TryDecode(
                WorkoutSnapshotCodec.Encode(original, SentAt), out var envelope);

            Assert.That(decoded, Is.True);
            Assert.That(ToJson(envelope.Snapshot), Is.EqualTo(ToJson(original)));
        }

        [Test]
        public void Encode_ThenTryDecode_PreservesSentTimeAsUtc()
        {
            WorkoutSnapshotCodec.TryDecode(
                WorkoutSnapshotCodec.Encode(CreateSnapshot(1), SentAt), out var envelope);

            Assert.That(envelope.SentAtUtc, Is.EqualTo(SentAt));
            Assert.That(envelope.SentAtUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(envelope.ProtocolVersion, Is.EqualTo(WorkoutSnapshotCodec.CurrentProtocolVersion));
        }

        [TestCase("")]
        [TestCase("not json")]
        [TestCase("{}")]
        [TestCase("null")]
        [TestCase("[1,2,3]")]
        public void TryDecode_WithInvalidPayload_ReturnsFalse(string text)
        {
            var decoded = WorkoutSnapshotCodec.TryDecode(Encoding.UTF8.GetBytes(text), out var envelope);

            Assert.That(decoded, Is.False);
            Assert.That(envelope, Is.Null);
        }

        [Test]
        public void TryDecode_WithNullPayload_ReturnsFalse()
        {
            Assert.That(WorkoutSnapshotCodec.TryDecode(null, out _), Is.False);
        }

        [Test]
        public void TryDecode_WithOtherProtocolVersion_ReturnsFalse()
        {
            var json = Encoding.UTF8.GetString(WorkoutSnapshotCodec.Encode(CreateSnapshot(1), SentAt))
                .Replace($"\"protocolVersion\":{WorkoutSnapshotCodec.CurrentProtocolVersion}", "\"protocolVersion\":99");

            Assert.That(WorkoutSnapshotCodec.TryDecode(Encoding.UTF8.GetBytes(json), out _), Is.False);
        }

        [Test]
        public void TryDecode_WithMissingExercises_ReturnsFalse()
        {
            const string json =
                "{\"protocolVersion\":1,\"sentAtUtc\":\"2026-01-01T12:00:00Z\"," +
                "\"snapshot\":{\"version\":1,\"name\":\"x\",\"rest\":{\"isStopped\":false}}}";

            Assert.That(WorkoutSnapshotCodec.TryDecode(Encoding.UTF8.GetBytes(json), out _), Is.False);
        }
    }
}
