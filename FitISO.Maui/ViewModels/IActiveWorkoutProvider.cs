using FitISO.Maui.Models;

namespace FitISO.Maui.ViewModels
{
    public interface IActiveWorkoutProvider
    {
        Workout Workout { get; }
    }
}