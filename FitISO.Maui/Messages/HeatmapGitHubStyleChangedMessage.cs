using CommunityToolkit.Mvvm.Messaging.Messages;

namespace FitISO.Maui.Messages
{
    public class HeatmapGitHubStyleChangedMessage : ValueChangedMessage<bool>
    {
        public HeatmapGitHubStyleChangedMessage(bool value) : base(value) { }
    }
}
