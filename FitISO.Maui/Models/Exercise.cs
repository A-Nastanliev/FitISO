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
        [NotifyPropertyChangedFor(nameof(VisibleSets))]
        Set? bestSet;

        [ObservableProperty]
        DateTime? lastSetsDate;

        [ObservableProperty]
        ObservableCollection<Set> lastSets;

        public bool VisibleSets => BestSet is { Weight: >= 0, Reps: > 0 };

        [ObservableProperty]
        ObservableCollection<ExerciseHistoryPoint> history = new();

        public bool HistoryLoaded { get; set; }

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
        }
    }
}
