using FitISO.Maui.ViewModels;

namespace FitISO.Maui.Views;

public partial class WorkoutTemplatesPage : ContentPage
{
    readonly WorkoutTemplatesViewModel viewModel;

	public WorkoutTemplatesPage(WorkoutTemplatesViewModel workoutTemplatesViewModel)
	{
		InitializeComponent();
		BindingContext = workoutTemplatesViewModel;
        viewModel = workoutTemplatesViewModel;
	}

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.LoadCommand.ExecuteAsync(null);
    }
}