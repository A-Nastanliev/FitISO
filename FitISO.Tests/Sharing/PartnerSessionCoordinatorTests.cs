using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FitISO.Sharing.Protocol;
using FitISO.Sharing.Session;
using FitISO.Sharing.Snapshots;
using FitISO.Sharing.Transport;
using NUnit.Framework;

namespace FitISO.Tests.Sharing
{
    [TestFixture]
    public class PartnerSessionCoordinatorTests
    {
        private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 10, DateTimeKind.Utc);

        private FakeWorkoutSessionTransport _transport;
        private PartnerSessionCoordinator _coordinator;
        private int _partnerUpdates;

        [SetUp]
        public void SetUp()
        {
            _transport = new FakeWorkoutSessionTransport();
            _coordinator = new PartnerSessionCoordinator(_transport, () => Now);
            _partnerUpdates = 0;
            _coordinator.PartnerUpdated += (_, _) => _partnerUpdates++;
        }

        [TearDown]
        public void TearDown()
        {
            _coordinator.Dispose();
        }

        private static WorkoutSnapshot CreateSnapshot(long version, string name = "Workout") => new(
            version,
            name,
            Now,
            null,
            new List<ExerciseSnapshot>(),
            new RestStopwatchSnapshot(null, false));

        private static byte[] CreatePayload(long version, string name = "Workout", DateTime? sentAt = null) =>
            WorkoutSnapshotCodec.Encode(CreateSnapshot(version, name), sentAt ?? Now);

        [Test]
        public void ReceivingNewerSnapshot_ReplacesPartnerSnapshot()
        {
            _transport.RaisePayload(CreatePayload(1, "first"));
            _transport.RaisePayload(CreatePayload(2, "second"));

            Assert.That(_coordinator.PartnerSnapshot.Name, Is.EqualTo("second"));
            Assert.That(_partnerUpdates, Is.EqualTo(2));
        }

        [Test]
        public void ReceivingDuplicateSnapshot_IsIgnored()
        {
            _transport.RaisePayload(CreatePayload(3));
            _transport.RaisePayload(CreatePayload(3));

            Assert.That(_partnerUpdates, Is.EqualTo(1));
        }

        [Test]
        public void ReceivingOlderSnapshot_KeepsNewerOne()
        {
            _transport.RaisePayload(CreatePayload(5, "newer"));
            _transport.RaisePayload(CreatePayload(4, "older"));

            Assert.That(_coordinator.PartnerSnapshot.Name, Is.EqualTo("newer"));
            Assert.That(_partnerUpdates, Is.EqualTo(1));
        }

        [Test]
        public void ReceivingGarbage_IsIgnored()
        {
            _transport.RaisePayload(new byte[] { 1, 2, 3 });

            Assert.That(_coordinator.PartnerSnapshot, Is.Null);
            Assert.That(_partnerUpdates, Is.EqualTo(0));
        }

        [Test]
        public void ClockOffset_WhenPartnerClockIsBehind_IsPositiveAndConvertsPartnerTimes()
        {
            var partnerSentAt = Now.AddSeconds(-10);

            _transport.RaisePayload(CreatePayload(1, sentAt: partnerSentAt));

            Assert.That(_coordinator.ClockOffset, Is.EqualTo(TimeSpan.FromSeconds(10)));
            Assert.That(_coordinator.PartnerTimeToLocal(partnerSentAt), Is.EqualTo(Now));
        }

        [Test]
        public void ResetPartner_ClearsSnapshotAndAcceptsLowerVersionsAgain()
        {
            _transport.RaisePayload(CreatePayload(100, "old partner"));

            _coordinator.ResetPartner();
            Assert.That(_coordinator.PartnerSnapshot, Is.Null);

            _transport.RaisePayload(CreatePayload(1, "new partner"));
            Assert.That(_coordinator.PartnerSnapshot.Name, Is.EqualTo("new partner"));
        }

        [Test]
        public async Task PublishAsync_WhenConnected_SendsSnapshot()
        {
            _transport.RaiseState(TransportState.Connected);

            await _coordinator.PublishAsync(CreateSnapshot(1));

            Assert.That(_transport.Sent, Has.Count.EqualTo(1));
            Assert.That(WorkoutSnapshotCodec.TryDecode(_transport.Sent[0], out var envelope), Is.True);
            Assert.That(envelope.Snapshot.Version, Is.EqualTo(1));
        }

        [Test]
        public async Task PublishAsync_WhenDisconnected_SendsOnlyLatestOnceConnected()
        {
            await _coordinator.PublishAsync(CreateSnapshot(1));
            await _coordinator.PublishAsync(CreateSnapshot(2));
            Assert.That(_transport.Sent, Is.Empty);

            _transport.RaiseState(TransportState.Connected);

            Assert.That(_transport.Sent, Has.Count.EqualTo(1));
            WorkoutSnapshotCodec.TryDecode(_transport.Sent[0], out var envelope);
            Assert.That(envelope.Snapshot.Version, Is.EqualTo(2));
        }

        [Test]
        public async Task Reconnecting_ResendsLatestSnapshot()
        {
            _transport.RaiseState(TransportState.Connected);
            await _coordinator.PublishAsync(CreateSnapshot(1));

            _transport.RaiseState(TransportState.Disconnected);
            _transport.RaiseState(TransportState.Connected);

            Assert.That(_transport.Sent, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task PublishAsync_WithSameOrOlderVersion_DoesNotSend()
        {
            _transport.RaiseState(TransportState.Connected);

            await _coordinator.PublishAsync(CreateSnapshot(5));
            await _coordinator.PublishAsync(CreateSnapshot(5));
            await _coordinator.PublishAsync(CreateSnapshot(4));

            Assert.That(_transport.Sent, Has.Count.EqualTo(1));
        }

        [Test]
        public void PublishAsync_WithNullSnapshot_ThrowsArgumentNullException()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => _coordinator.PublishAsync(null));
        }

        [Test]
        public void TransportStateChange_IsForwardedToConnectionStateChanged()
        {
            TransportState? seen = null;
            _coordinator.ConnectionStateChanged += (_, state) => seen = state;

            _transport.RaiseState(TransportState.Connecting);

            Assert.That(seen, Is.EqualTo(TransportState.Connecting));
        }
    }
}
