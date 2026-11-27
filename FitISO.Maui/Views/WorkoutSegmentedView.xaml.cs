using CommunityToolkit.Maui.Behaviors;
using FitISO.Maui.ViewModels;

namespace FitISO.Maui.Views
{
    public partial class WorkoutSegmentedView : ContentView
    {
        public WorkoutSegmentedView()
        {
            InitializeComponent();
        }

        public void SetRestStopwatch(RestStopwatchViewModel restStopwatch)
        {
            OwnRestTile.BindingContext = restStopwatch;
            OwnRestTile.Behaviors.Add(new TouchBehavior
            {
                Command = restStopwatch.ToggleCommand,
                LongPressCommand = restStopwatch.ResetCommand,
                LongPressDuration = 600
            });
        }
    }
}