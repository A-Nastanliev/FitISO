using CommunityToolkit.Mvvm.ComponentModel;

namespace FitISO.Maui
{
    public partial class ActiveWorkoutState : ObservableObject
    {
        public static ActiveWorkoutState Instance { get; } = new();

        private ActiveWorkoutState()
        {
        }

        [ObservableProperty]
        bool hasActiveWorkout;

        [ObservableProperty]
        bool isActiveWorkoutPageVisible;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotInPipMode))]
        bool isInPipMode;

        public bool IsNotInPipMode => !IsInPipMode;
    }
}