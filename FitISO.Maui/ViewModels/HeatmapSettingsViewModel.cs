using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Maui.Services;

namespace FitISO.Maui.ViewModels
{
    public partial class HeatmapSettingsViewModel : ObservableObject, IRecipient<HeatmapWeekStartsOnMondayChangedMessage>,
        IRecipient<HeatmapGitHubStyleChangedMessage>
    {
        [ObservableProperty]
        bool heatmapWeekStartsOnMonday;

        [ObservableProperty]
        bool heatmapGitHubStyle;

        readonly HeatmapSettingsService heatmapSettingsService;

        public HeatmapSettingsViewModel(HeatmapSettingsService heatmapSettingsService)
        {
            this.heatmapSettingsService = heatmapSettingsService;

            WeakReferenceMessenger.Default.RegisterAll(this);

            HeatmapWeekStartsOnMonday = heatmapSettingsService.WeekStartsOnMonday;
            HeatmapGitHubStyle = heatmapSettingsService.GitHubStyle;
        }

        partial void OnHeatmapWeekStartsOnMondayChanged(bool value) => heatmapSettingsService.WeekStartsOnMonday = value;

        partial void OnHeatmapGitHubStyleChanged(bool value) => heatmapSettingsService.GitHubStyle = value;

        public void Receive(HeatmapWeekStartsOnMondayChangedMessage message) => HeatmapWeekStartsOnMonday = message.Value;

        public void Receive(HeatmapGitHubStyleChangedMessage message) => HeatmapGitHubStyle = message.Value;
    }
}
