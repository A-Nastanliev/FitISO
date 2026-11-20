using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Maui.Services;

namespace FitISO.Maui.ViewModels
{
    public partial class VoiceLoggingSettingsViewModel : ObservableObject, IRecipient<VoiceLoggingEnabledChangedMessage>
    {
        [ObservableProperty]
        bool voiceLoggingEnabled;

        readonly VoiceLoggingSettingsService voiceLoggingSettingsService;

        public VoiceLoggingSettingsViewModel(VoiceLoggingSettingsService voiceLoggingSettingsService)
        {
            this.voiceLoggingSettingsService = voiceLoggingSettingsService;

            WeakReferenceMessenger.Default.RegisterAll(this);

            VoiceLoggingEnabled = voiceLoggingSettingsService.VoiceLoggingEnabled;
        }

        partial void OnVoiceLoggingEnabledChanged(bool value) => voiceLoggingSettingsService.VoiceLoggingEnabled = value;

        public void Receive(VoiceLoggingEnabledChangedMessage message) => VoiceLoggingEnabled = message.Value;
    }
}