using FitISO.Maui.Views;

namespace FitISO.Maui
{
    public partial class AppShell : Shell
    {
        public static string? PendingRoute { get; set; }

        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(WorkoutFormPage), typeof(WorkoutFormPage));

            if (PendingRoute is { } route)
            {
                PendingRoute = null;

                var target = ((TabBar)Items[0]).Items.FirstOrDefault(t => t.Route == route);
                if (target is not null)
                {
                    CurrentItem = target;
                }
            }
        }
    }
}
