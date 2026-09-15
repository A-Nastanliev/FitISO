using CommunityToolkit.Mvvm.Messaging.Messages;

namespace FitISO.Maui.Messages
{
    public class AccentThemeChangedMessage : ValueChangedMessage<string>
    {
        public AccentThemeChangedMessage(string themeName) : base(themeName) { }
    }
}