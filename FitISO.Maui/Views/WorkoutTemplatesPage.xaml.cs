namespace FitISO.Maui.Views;

public partial class WorkoutTemplatesPage : ContentPage
{
	public WorkoutTemplatesPage()
	{
		InitializeComponent();
	}

    private void ToggleActiveWorkoutClicked(object? sender, EventArgs e)
    {
        ActiveWorkoutState.Instance.HasActiveWorkout = !ActiveWorkoutState.Instance.HasActiveWorkout;
    }
}