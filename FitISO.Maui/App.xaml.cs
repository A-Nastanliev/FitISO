using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Services;
using FitISO.Maui.Resources.Styles.AccentThemes;
using Microsoft.Extensions.DependencyInjection;

namespace FitISO.Maui
{
    public partial class App : Application
    {
        private readonly WorkoutService _workoutService;

        public const string DatabaseFileName = "fitiso.db3";

        public static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);

        public App(WorkoutService workoutService)
        {
            InitializeComponent();
            _workoutService = workoutService;

            ApplySavedAccentTheme();
        }

        private static void ApplySavedAccentTheme()
        {
            var savedTheme = Preferences.Get("accent_theme", nameof(Default));

            ResourceDictionary theme = savedTheme switch
            {
                nameof(DarkBlue) => new DarkBlue(),
                nameof(DarkRed) => new DarkRed(),
                nameof(Olive)=> new Olive(),
                _ => new Default()
            };

            var existing = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d is Default);

            if (existing != null)
                Application.Current.Resources.MergedDictionaries.Remove(existing);

            Application.Current.Resources.MergedDictionaries.Add(theme);
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

            window.Created += async (sender, args) =>
            {
                var activeWorkout = await _workoutService.GetActiveWorkoutAsync();

                if (activeWorkout != null && activeWorkout?.Id != 0)
                {
                    WeakReferenceMessenger.Default.Send(new WorkoutStartedMessage(new FitISO.Maui.Models.Workout(activeWorkout)));
                    ActiveWorkoutState.Instance.HasActiveWorkout = true;
                }
            };

            return window;
        }
    }
}