using FitISO.Maui.Models;
using System.ComponentModel;

namespace FitISO.Maui.ViewModels
{
    public interface IActiveWorkoutProvider : INotifyPropertyChanged
    {
        Workout Workout { get; }
    }
}