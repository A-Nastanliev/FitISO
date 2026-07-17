using FitISO.Maui.ViewModels;

namespace FitISO.Maui.Views;

public partial class SettingsPage : ContentPage
{
	public SettingsPage(SettingsPageViewModel settingsPageViewModel)
	{
		InitializeComponent();
		BindingContext = settingsPageViewModel;
	}
}