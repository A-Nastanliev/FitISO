namespace FitISO.Sharing.Transport
{
    public sealed record PairingRequest(string PartnerName, string Code, Action Accept, Action Reject);
}