using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitISO.Maui.Models
{
    public partial class Exercise : ObservableObject
    {
        [ObservableProperty]
        int id;

        [ObservableProperty]
        string name;

        [ObservableProperty]
        Set? bestSet;

        [ObservableProperty]
        DateTime? lastSetsDate;

        [ObservableProperty]
        ObservableCollection<Set> lastSets;

        [ObservableProperty]
        bool visibleSets;

        [ObservableProperty]
        ObservableCollection<ExerciseHistoryPoint> history = new();

        public Exercise()
        {

        }

        public Exercise(FitISO.Data.Models.Exercise exercise)
        {
            Id = exercise.Id;
            Name = exercise.Name;
            BestSet = exercise.BestSet != null ? new Set(exercise.BestSet) : null;
            LastSetsDate = exercise.LastSetsDate;
            LastSets = new ObservableCollection<Set>((exercise.LastSets ?? new List<FitISO.Data.Models.Set>()).Select(s => new Set(s)));
            VisibleSets = bestSet is not null && bestSet?.Reps > 0 && bestSet?.Weight > 0;
        }
    }
}
