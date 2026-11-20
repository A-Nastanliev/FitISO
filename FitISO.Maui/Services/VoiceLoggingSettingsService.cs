using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;

namespace FitISO.Maui.Services
{
    public class VoiceLoggingSettingsService
    {
        const string VoiceLoggingEnabledKey = "VoiceLoggingEnabled";

        public bool VoiceLoggingEnabled
        {
            get => Preferences.Default.Get(VoiceLoggingEnabledKey, false);
            set
            {
                if (VoiceLoggingEnabled == value) return;
                Preferences.Default.Set(VoiceLoggingEnabledKey, value);
                WeakReferenceMessenger.Default.Send(new VoiceLoggingEnabledChangedMessage(value));
            }
        }
    }
}