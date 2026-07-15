namespace FitISO.Maui.Views;

public partial class ActiveWorkoutPage : ContentPage
{
	public ActiveWorkoutPage()
	{
		InitializeComponent();
	}
    private void ToggleActiveWorkoutClicked(object? sender, EventArgs e)
    {
        ActiveWorkoutState.Instance.HasActiveWorkout = !ActiveWorkoutState.Instance.HasActiveWorkout;
    }
}