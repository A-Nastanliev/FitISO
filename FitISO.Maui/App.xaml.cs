using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Maui.Models;
using FitISO.Maui.Resources.Styles.AccentThemes;
using FitISO.Maui.Services;
using FitISO.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FitISO.Maui
{
    public partial class App : Application, IRecipient<DynamicAccentColorsChangedMessage>
    {
        private readonly WorkoutService _workoutService;
        private readonly AccentThemeService _accentThemeService;

        public const string DatabaseFileName = "fitiso.db3";

        public static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);

        public App(WorkoutService workoutService, AccentThemeService accentThemeService)
        {
            InitializeComponent();
            _workoutService = workoutService;
            _accentThemeService = accentThemeService;

            ApplySavedAccentTheme(accentThemeService);

            WeakReferenceMessenger.Default.Register<DynamicAccentColorsChangedMessage>(this);
        }

        public void Receive(DynamicAccentColorsChangedMessage message)
        {
            if (_accentThemeService.AccentThemeName != "Dynamic")
                return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var existing = Application.Current!.Resources.MergedDictionaries
                    .FirstOrDefault(d => d is DynamicAccentTheme);
                if (existing != null)
                    Application.Current.Resources.MergedDictionaries.Remove(existing);

                Application.Current.Resources.MergedDictionaries.Add(new DynamicAccentTheme());
            });
        }

        private static void ApplySavedAccentTheme(AccentThemeService accentThemeService)
        {
            var savedTheme = accentThemeService.AccentThemeName;

            ResourceDictionary theme = savedTheme switch
            {
                "Dynamic" => DynamicAccentTheme.IsAvailable ? new DynamicAccentTheme() : new Default(),
                nameof(DarkBlue) => new DarkBlue(),
                nameof(DarkRed) => new DarkRed(),
                nameof(Olive) => new Olive(),
                nameof(DeepTeal) => new DeepTeal(),
                nameof(Slate) => new Slate(),
                nameof(Espresso) => new Espresso(),
                nameof(Rust) => new Rust(),
                nameof(Plum) => new Plum(),
                nameof(Forest) => new Forest(),
                nameof(Midnight) => new Midnight(),
                nameof(Amber) => new Amber(),
                nameof(Ink) => new Ink(),
                nameof(Mauve) => new Mauve(),
                nameof(Wine) => new Wine(),
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