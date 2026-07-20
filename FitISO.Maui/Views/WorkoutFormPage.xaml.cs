using FitISO.Maui.ViewModels;

namespace FitISO.Maui.Views;

public partial class WorkoutFormPage : ContentPage
{
	readonly WorkoutFormViewModel viewModel;
	
	public WorkoutFormPage(WorkoutFormViewModel workoutFormViewModel)
	{
		InitializeComponent();
		BindingContext = workoutFormViewModel;
		viewModel = workoutFormViewModel;
	}

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.ExerciseCollection.LoadCommand.ExecuteAsync(null);
    }
}