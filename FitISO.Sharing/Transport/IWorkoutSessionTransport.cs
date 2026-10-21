namespace FitISO.Sharing.Transport
{
    public interface IWorkoutSessionTransport
    {
        TransportState State { get; }

        event EventHandler<TransportState> StateChanged;

        event EventHandler<byte[]> PayloadReceived;

        Task SendAsync(byte[] payload, CancellationToken cancellationToken = default);
    }
}