using CommunityToolkit.Mvvm.Messaging.Messages;

namespace FitISO.Maui.Messages
{
    public class RestStopwatchEnabledChangedMessage : ValueChangedMessage<bool>
    {
        public RestStopwatchEnabledChangedMessage(bool value) : base(value)
        {
        }
    }
}