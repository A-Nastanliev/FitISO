using FitISO.Maui.ViewModels;

namespace FitISO.Maui.Views;

public partial class ActiveWorkoutPage : ContentPage
{
	readonly ActiveWorkoutViewModel viewModel;

	public ActiveWorkoutPage(ActiveWorkoutViewModel activeWorkoutViewModel)
	{
		InitializeComponent();
		BindingContext = activeWorkoutViewModel;
		viewModel = activeWorkoutViewModel;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.EnsureTimerRunning();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        viewModel.Stop();
    }
}