using FitISO.Maui.ViewModels;

namespace FitISO.Maui.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(AccentThemeSettingsViewModel accentTheme, BackupSettingsViewModel backup,
        WorkoutSettingsViewModel workoutSettings, HeatmapSettingsViewModel heatmapSettings)
    {
        InitializeComponent();

        AccentSection.BindingContext = accentTheme;
        BackupSection.BindingContext = backup;
        WorkoutSettingsSection.BindingContext = workoutSettings;
        HeatmapSection.BindingContext = heatmapSettings;
    }
}