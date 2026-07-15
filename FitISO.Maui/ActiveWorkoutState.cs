using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FitISO.Maui;
public class ActiveWorkoutState : INotifyPropertyChanged
{
    public static ActiveWorkoutState Instance { get; } = new();

    private ActiveWorkoutState()
    {
    }

    private bool _hasActiveWorkout;

    public bool HasActiveWorkout
    {
        get => _hasActiveWorkout;
        set
        {
            if (_hasActiveWorkout == value)
            {
                return;
            }

            _hasActiveWorkout = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}