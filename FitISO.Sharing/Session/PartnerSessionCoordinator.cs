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
        long _localEndedVersion;

        WorkoutSnapshot _partner;
        long _partnerLastVersion;
        bool _partnerFinished;
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

        public bool PartnerFinished
        {
            get { lock (_gate) return _partnerFinished; }
        }

        public TimeSpan ClockOffset
        {
            get { lock (_gate) return _clockOffset; }
        }

        public DateTime PartnerTimeToLocal(DateTime partnerUtc) => partnerUtc + ClockOffset;

        long LatestLocalVersion => Math.Max(_latestLocal?.Version ?? 0, _localEndedVersion);

        public Task PublishAsync(WorkoutSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            lock (_gate)
            {
                if (snapshot.Version <= LatestLocalVersion)
                    return Task.CompletedTask;
                _latestLocal = snapshot;
            }
            return SendLatestAsync();
        }

        public Task PublishSessionEndedAsync(long version)
        {
            lock (_gate)
            {
                if (version <= LatestLocalVersion)
                    return Task.CompletedTask;
                _localEndedVersion = version;
            }
            return SendLatestAsync();
        }

        public void ResetPartner()
        {
            lock (_gate)
            {
                _partner = null;
                _partnerLastVersion = 0;
                _partnerFinished = false;
            }
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

                if (envelope.Type == MessageType.SessionEnded)
                {
                    if (envelope.EndedVersion > _partnerLastVersion)
                    {
                        _partnerLastVersion = envelope.EndedVersion;

                        if (_partner != null)
                        {
                            _partnerFinished = true;
                            changed = true;
                        }
                    }
                }
                else if (envelope.Snapshot.Version > _partnerLastVersion)
                {
                    _partner = envelope.Snapshot;
                    _partnerLastVersion = envelope.Snapshot.Version;
                    _partnerFinished = false;
                    changed = true;
                }
            }

            if (changed)
                PartnerUpdated?.Invoke(this, EventArgs.Empty);
        }

        async Task SendLatestAsync()
        {
            byte[] payload;
            lock (_gate)
            {
                if (_transport.State != TransportState.Connected)
                    return;

                long snapshotVersion = _latestLocal?.Version ?? 0;

                if (_localEndedVersion > snapshotVersion)
                    payload = WorkoutSnapshotCodec.EncodeSessionEnded(_localEndedVersion, _utcNow());
                else if (_latestLocal != null)
                    payload = WorkoutSnapshotCodec.Encode(_latestLocal, _utcNow());
                else
                    return;
            }

            try
            {
                await _transport.SendAsync(payload);
            }
            catch (Exception)
            {
            }
        }
    }
}