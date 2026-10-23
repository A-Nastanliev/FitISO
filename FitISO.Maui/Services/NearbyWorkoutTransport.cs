#if ANDROID
using global::Android.Gms.Common.Apis;
using global::Android.Gms.Extensions;
using global::Android.Gms.Nearby;
using global::Android.Gms.Nearby.Connection;
using FitISO.Sharing.Transport;
using NearbyStrategy = global::Android.Gms.Nearby.Connection.Strategy;

namespace FitISO.Maui.Services
{
    public sealed class NearbyWorkoutTransport : IPairingTransport
    {
        const string ServiceId = "com.fitiso.maui.traintogether";
        static readonly TimeSpan ReconnectWindow = TimeSpan.FromSeconds(30);

        readonly IConnectionsClient _client;
        readonly string _localId = Guid.NewGuid().ToString("N")[..8];
        readonly LifecycleCallback _lifecycle;
        readonly DiscoveryCallback _discovery;
        readonly PayloadHandler _payloads;

        string? _endpointId;     
        string? _partnerId;
        string? _pendingName;  
        string? _pendingId;
        bool _running;
        bool _stopping;
        CancellationTokenSource? _reconnectCts;

        public NearbyWorkoutTransport()
        {
            _client = NearbyClass.GetConnectionsClient(global::Android.App.Application.Context);
            _lifecycle = new LifecycleCallback(this);
            _discovery = new DiscoveryCallback(this);
            _payloads = new PayloadHandler(this);
        }

        public TransportState State { get; private set; } = TransportState.Disconnected;
        public string? PartnerName { get; private set; }
        public bool ConnectionLost { get; private set; }

        public event EventHandler<TransportState>? StateChanged;
        public event EventHandler<byte[]>? PayloadReceived;
        public event EventHandler<PairingRequest>? PairingRequested;

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_running) return;

            _running = true;
            _stopping = false;
            ConnectionLost = false;
            _partnerId = null;
            PartnerName = null;

            SetState(TransportState.Connecting);

            try
            {
                await StartRadiosAsync();
            }
            catch
            {
                await StopAsync();
                throw;
            }
        }

        public Task StopAsync()
        {
            _stopping = true;
            _running = false;
            _reconnectCts?.Cancel();

            StopRadios();
            _client.StopAllEndpoints();

            _endpointId = null;
            _partnerId = null;
            _pendingName = null;
            _pendingId = null;
            PartnerName = null;
            ConnectionLost = false;

            SetState(TransportState.Disconnected);
            return Task.CompletedTask;
        }

        public Task ReconnectAsync()
        {
            if (!_running || _partnerId is null) return Task.CompletedTask;

            ConnectionLost = false;
            BeginReconnect();
            return Task.CompletedTask;
        }

        public async Task SendAsync(byte[] payload, CancellationToken cancellationToken = default)
        {
            var endpoint = _endpointId;
            if (endpoint is null || State != TransportState.Connected)
                throw new InvalidOperationException("Not connected.");

            await _client.SendPayload(endpoint, Payload.FromBytes(payload)).AsAsync();
        }

        async Task StartRadiosAsync()
        {
            var name = $"{DeviceInfo.Name}|{_localId}";

            var advertising = new AdvertisingOptions.Builder().SetStrategy(NearbyStrategy.P2pPointToPoint).Build();
            var discovering = new DiscoveryOptions.Builder().SetStrategy(NearbyStrategy.P2pPointToPoint).Build();

            try
            {
                await _client.StartAdvertising(name, ServiceId, _lifecycle, advertising).AsAsync();
            }
            catch (ApiException ex) when (ex.StatusCode == ConnectionsStatusCodes.StatusAlreadyAdvertising) { }

            try
            {
                await _client.StartDiscovery(ServiceId, _discovery, discovering).AsAsync();
            }
            catch (ApiException ex) when (ex.StatusCode == ConnectionsStatusCodes.StatusAlreadyDiscovering) { }
        }

        void StopRadios()
        {
            _client.StopAdvertising();
            _client.StopDiscovery();
        }

        void BeginReconnect()
        {
            SetState(TransportState.Connecting);

            _reconnectCts?.Cancel();
            var cts = _reconnectCts = new CancellationTokenSource();
            _ = ReconnectLoopAsync(cts.Token);
        }

        async Task ReconnectLoopAsync(CancellationToken token)
        {
            try
            {
                await StartRadiosAsync();
                await Task.Delay(ReconnectWindow, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }
            catch (Exception)
            {
            }

            if (token.IsCancellationRequested || State == TransportState.Connected) return;

            StopRadios();
            ConnectionLost = true;
            SetState(TransportState.Disconnected);
        }

        static (string Name, string Id) ParseEndpointName(string endpointName)
        {
            int i = endpointName.LastIndexOf('|');
            return i < 0 ? (endpointName, endpointName) : (endpointName[..i], endpointName[(i + 1)..]);
        }

        void OnEndpointFound(string endpointId, DiscoveredEndpointInfo info)
        {
            if (_endpointId is not null) return;

            var (_, remoteId) = ParseEndpointName(info.EndpointName);

            if (_partnerId is not null && remoteId != _partnerId) return;

            if (string.CompareOrdinal(_localId, remoteId) >= 0) return;

            _client.RequestConnection($"{DeviceInfo.Name}|{_localId}", endpointId, _lifecycle);
        }

        void OnConnectionInitiated(string endpointId, ConnectionInfo info)
        {
            if (_endpointId is not null)
            {
                _client.RejectConnection(endpointId);
                return;
            }

            var (remoteName, remoteId) = ParseEndpointName(info.EndpointName);
            _pendingName = remoteName;
            _pendingId = remoteId;

            if (_partnerId is not null && remoteId == _partnerId)
            {
                _client.AcceptConnection(endpointId, _payloads);
                return;
            }

            var handler = PairingRequested;
            if (handler is null)
            {
                _client.RejectConnection(endpointId);
                return;
            }

            handler(this, new PairingRequest(
                remoteName,
                info.AuthenticationDigits,
                Accept: () => _client.AcceptConnection(endpointId, _payloads),
                Reject: () => _client.RejectConnection(endpointId)));
        }

        void OnConnectionResult(string endpointId, ConnectionResolution resolution)
        {
            if (resolution.Status.StatusCode != ConnectionsStatusCodes.StatusOk)
            {
                _pendingName = null;
                _pendingId = null;
                return;
            }

            if (_pendingId is not null)
            {
                PartnerName = _pendingName;
                _partnerId = _pendingId;
            }
            _pendingName = null;
            _pendingId = null;

            _reconnectCts?.Cancel();
            StopRadios();

            _endpointId = endpointId;
            ConnectionLost = false;

            SetState(TransportState.Connected);
        }

        void OnEndpointDisconnected(string endpointId)
        {
            if (endpointId != _endpointId) return;

            _endpointId = null;

            if (_stopping || !_running)
            {
                SetState(TransportState.Disconnected);
                return;
            }

            BeginReconnect();
        }

        void OnBytes(byte[] bytes) => PayloadReceived?.Invoke(this, bytes);

        void SetState(TransportState state)
        {
            if (State == state) return;
            State = state;
            StateChanged?.Invoke(this, state);
        }

        sealed class LifecycleCallback : ConnectionLifecycleCallback
        {
            readonly NearbyWorkoutTransport _owner;
            public LifecycleCallback(NearbyWorkoutTransport owner) => _owner = owner;

            public override void OnConnectionInitiated(string endpointId, ConnectionInfo info)
            {
                _owner.OnConnectionInitiated(endpointId, info);
            }

            public override void OnConnectionResult(string endpointId, ConnectionResolution resolution) =>
                _owner.OnConnectionResult(endpointId, resolution);

            public override void OnDisconnected(string endpointId) =>
                _owner.OnEndpointDisconnected(endpointId);
        }

        sealed class DiscoveryCallback : EndpointDiscoveryCallback
        {
            readonly NearbyWorkoutTransport _owner;
            public DiscoveryCallback(NearbyWorkoutTransport owner) => _owner = owner;

            public override void OnEndpointFound(string endpointId, DiscoveredEndpointInfo info) =>
                _owner.OnEndpointFound(endpointId, info);

            public override void OnEndpointLost(string endpointId) { }
        }

        sealed class PayloadHandler : PayloadCallback
        {
            readonly NearbyWorkoutTransport _owner;
            public PayloadHandler(NearbyWorkoutTransport owner) => _owner = owner;

            public override void OnPayloadReceived(string endpointId, Payload payload)
            {
                if (payload.PayloadType == Payload.Type.Bytes && payload.AsBytes() is { } bytes)
                    _owner.OnBytes(bytes);
            }

            public override void OnPayloadTransferUpdate(string endpointId, PayloadTransferUpdate update) { }
        }
    }
}
#endif