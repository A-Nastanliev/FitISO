using FitISO.Maui.ViewModels;

namespace FitISO.Maui.Views
{
    public partial class WorkoutCompactBarView : ContentView
    {
        public WorkoutCompactBarView()
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