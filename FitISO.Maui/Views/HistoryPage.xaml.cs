using FitISO.Maui.ViewModels;

namespace FitISO.Maui.Views;

public partial class HistoryPage : ContentPage
{
    readonly HistoryPageViewModel viewModel;

    public HistoryPage(HistoryPageViewModel historyPageViewModel, HeatmapViewModel heatmapViewModel)
    {
        InitializeComponent();
        BindingContext = historyPageViewModel;
        viewModel = historyPageViewModel;

        HeatmapSection.Attach(heatmapViewModel);
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.LoadFirst();
        await HeatmapSection.RefreshIfStaleAsync();
    }
}
