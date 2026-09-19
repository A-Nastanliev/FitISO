using CommunityToolkit.Mvvm.Messaging.Messages;

namespace FitISO.Maui.Messages
{
    public class ProgressCardEnabledChangedMessage : ValueChangedMessage<bool>
    {
        public ProgressCardEnabledChangedMessage(bool value) : base(value)
        {
        }
    }
}