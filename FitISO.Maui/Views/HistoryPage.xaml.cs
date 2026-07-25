using FitISO.Maui.ViewModels;

namespace FitISO.Maui.Views;

public partial class HistoryPage : ContentPage
{
	readonly HistoryPageViewModel viewModel;

	public HistoryPage(HistoryPageViewModel historyPageViewModel)
	{
		InitializeComponent();
		BindingContext = historyPageViewModel;
		viewModel = historyPageViewModel;
	}

    protected async override void OnAppearing()
    {
        base.OnAppearing();
		await viewModel.Load();
    }
}