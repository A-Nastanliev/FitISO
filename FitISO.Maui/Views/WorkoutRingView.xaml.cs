using FitISO.Maui.ViewModels;

namespace FitISO.Maui.Views
{

    public partial class WorkoutRingView : ContentView
    {
        public WorkoutRingView()
        {
            InitializeComponent();
        }

        public void SetRestStopwatch(RestStopwatchViewModel restStopwatch)
        {
            RestTimer.BindingContext = restStopwatch;
            RestControls.BindingContext = restStopwatch;
        }
    }
}