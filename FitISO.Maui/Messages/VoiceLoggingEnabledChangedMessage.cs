using CommunityToolkit.Mvvm.Messaging.Messages;

namespace FitISO.Maui.Messages
{
    public class VoiceLoggingEnabledChangedMessage(bool value) : ValueChangedMessage<bool>(value);
}