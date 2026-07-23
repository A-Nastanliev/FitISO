using FitISO.Maui.ViewModels;

namespace FitISO.Maui.Views;

public partial class SelectExercisePopupPage : Nalu.PopupPageBase
{
    readonly SelectExercisePopupViewModel viewModel;

    public SelectExercisePopupPage(SelectExercisePopupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        this.viewModel = viewModel;
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.EnsureLoaded();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        viewModel.CompleteIfNotAlready();
    }
}
