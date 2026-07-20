using FitISO.Maui.Views;

namespace FitISO.Maui
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(WorkoutFormPage), typeof(WorkoutFormPage));
        }
    }
}
