using FitISO.Maui.ViewModels;

namespace FitISO.Maui.Views;

public partial class AddExercisePopupPage : Nalu.PopupPageBase
{
    readonly AddExercisePopupViewModel viewModel;
    public AddExercisePopupPage(AddExercisePopupViewModel viewModel)
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
}