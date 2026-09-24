using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;

namespace FitISO.Maui.Services
{
    public class HeatmapSettingsService
    {
        const string WeekStartsOnMondayKey = "HeatmapWeekStartsOnMonday";

        public bool WeekStartsOnMonday
        {
            get => Preferences.Default.Get(WeekStartsOnMondayKey, true);
            set
            {
                if (WeekStartsOnMonday == value) return;
                Preferences.Default.Set(WeekStartsOnMondayKey, value);
                WeakReferenceMessenger.Default.Send(new HeatmapWeekStartsOnMondayChangedMessage(value));
            }
        }

        public DayOfWeek WeekStartDay => WeekStartsOnMonday ? DayOfWeek.Monday : DayOfWeek.Sunday;

        const string GitHubStyleKey = "HeatmapGitHubStyle";

        public bool GitHubStyle
        {
            get => Preferences.Default.Get(GitHubStyleKey, false);
            set
            {
                if (GitHubStyle == value) return;
                Preferences.Default.Set(GitHubStyleKey, value);
                WeakReferenceMessenger.Default.Send(new HeatmapGitHubStyleChangedMessage(value));
            }
        }
    }
}