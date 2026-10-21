using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitISO.Sharing.Transport;

namespace FitISO.Tests.Sharing
{
    internal class FakeWorkoutSessionTransport : IWorkoutSessionTransport
    {
        public TransportState State { get; private set; } = TransportState.Disconnected;
        public List<byte[]> Sent { get; } = new();

        public event EventHandler<TransportState> StateChanged;
        public event EventHandler<byte[]> PayloadReceived;

        public Task SendAsync(byte[] payload, CancellationToken cancellationToken = default)
        {
            Sent.Add(payload);
            return Task.CompletedTask;
        }

        public void RaiseState(TransportState state)
        {
            State = state;
            StateChanged?.Invoke(this, state);
        }

        public void RaisePayload(byte[] payload) => PayloadReceived?.Invoke(this, payload);
    }
}
