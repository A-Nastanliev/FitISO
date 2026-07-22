using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Services;
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