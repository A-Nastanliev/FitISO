namespace FitISO.Sharing.Transport
{
    public interface IPairingTransport : IWorkoutSessionTransport
    {
        string? PartnerName { get; }

        bool ConnectionLost { get; }

        event EventHandler<PairingRequest> PairingRequested;

        Task StartAsync(CancellationToken cancellationToken = default);

        Task StopAsync();

        Task ReconnectAsync();
    }
}