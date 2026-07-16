using FitISO.Maui.ViewModels;

namespace FitISO.Maui.Views;

public partial class ExercisesPage : ContentPage
{
    readonly ExercisePageViewModel viewModel;

	public ExercisesPage(ExercisePageViewModel exercisePageViewModel)
	{
		InitializeComponent();
        BindingContext = exercisePageViewModel;
        viewModel = exercisePageViewModel;
	}

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.ExerciseCollection.LoadCommand.ExecuteAsync(null);
    }
}