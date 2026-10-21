using FitISO.Sharing.Protocol;
using FitISO.Sharing.Snapshots;
using FitISO.Sharing.Transport;

namespace FitISO.Sharing.Session
{
    public sealed class PartnerSessionCoordinator : IDisposable
    {
        readonly IWorkoutSessionTransport _transport;
        readonly Func<DateTime> _utcNow;
        readonly object _gate = new();

        WorkoutSnapshot _latestLocal;
        WorkoutSnapshot _partner;
        TimeSpan _clockOffset;

        public PartnerSessionCoordinator(IWorkoutSessionTransport transport, Func<DateTime> utcNow = null)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
            _transport.PayloadReceived += OnPayloadReceived;
            _transport.StateChanged += OnStateChanged;
        }

        public event EventHandler PartnerUpdated;

        public event EventHandler<TransportState> ConnectionStateChanged;

        public TransportState ConnectionState => _transport.State;

        public WorkoutSnapshot PartnerSnapshot
        {
            get { lock (_gate) return _partner; }
        }

        public TimeSpan ClockOffset
        {
            get { lock (_gate) return _clockOffset; }
        }

        public DateTime PartnerTimeToLocal(DateTime partnerUtc) => partnerUtc + ClockOffset;

        public Task PublishAsync(WorkoutSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            lock (_gate)
            {
                if (_latestLocal != null && snapshot.Version <= _latestLocal.Version)
                    return Task.CompletedTask;
                _latestLocal = snapshot;
            }
            return SendLatestAsync();
        }

        public void ResetPartner()
        {
            lock (_gate) _partner = null;
            PartnerUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            _transport.PayloadReceived -= OnPayloadReceived;
            _transport.StateChanged -= OnStateChanged;
        }

        void OnStateChanged(object sender, TransportState state)
        {
            ConnectionStateChanged?.Invoke(this, state);
            if (state == TransportState.Connected)
                _ = SendLatestAsync();
        }

        void OnPayloadReceived(object sender, byte[] payload)
        {
            if (!WorkoutSnapshotCodec.TryDecode(payload, out var envelope))
                return;

            var receivedAt = _utcNow();
            bool changed = false;
            lock (_gate)
            {
                _clockOffset = receivedAt - envelope.SentAtUtc;
                if (_partner == null || envelope.Snapshot.Version > _partner.Version)
                {
                    _partner = envelope.Snapshot;
                    changed = true;
                }
            }

            if (changed)
                PartnerUpdated?.Invoke(this, EventArgs.Empty);
        }

        async Task SendLatestAsync()
        {
            WorkoutSnapshot snapshot;
            lock (_gate) snapshot = _latestLocal;

            if (snapshot == null || _transport.State != TransportState.Connected)
                return;

            try
            {
                await _transport.SendAsync(WorkoutSnapshotCodec.Encode(snapshot, _utcNow()));
            }
            catch (Exception)
            {
            }
        }
    }
}