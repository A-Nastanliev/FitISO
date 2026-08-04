using FitISO.Maui.ViewModels;

namespace FitISO.Maui.Views;

public partial class ExerciseDetailsPopupPage : Nalu.PopupPageBase
{
    readonly ExerciseDetailsPopupViewModel viewModel;
    public ExerciseDetailsPopupPage(ExerciseDetailsPopupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        this.viewModel = viewModel;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        viewModel.CompleteIfNotAlready();
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.CheckIfDeletable();
        await viewModel.LoadChartData();
    }
}