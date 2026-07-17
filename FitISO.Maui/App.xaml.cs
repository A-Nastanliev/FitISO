using Microsoft.Extensions.DependencyInjection;

namespace FitISO.Maui
{
    public partial class App : Application
    {
        public const string DatabaseFileName = "fitiso.db3";

        public static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);

        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}