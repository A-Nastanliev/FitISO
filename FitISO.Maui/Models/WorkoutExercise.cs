using CommunityToolkit.Mvvm.ComponentModel;
using FitISO.Data.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace FitISO.Maui.Models
{
    public partial class WorkoutExercise : ObservableObject
    {
        [ObservableProperty]
        int id;

        [ObservableProperty]
        Exercise exercise;

        [ObservableProperty]
        string note;

        [ObservableProperty]
        ObservableCollection<Set> sets = new();

        [ObservableProperty]
        int setCount;

        public WorkoutExercise() { }

        public WorkoutExercise(FitISO.Data.Models.WorkoutExercise workoutExercise)
        {
            Id = workoutExercise.Id;
            Note = workoutExercise.Note;
            Exercise = new Exercise(workoutExercise.Exercise);
            foreach (var set in workoutExercise.Sets)
            {
                Sets.Add(new Set(set));
                SetCount++;
            }
        }
    }
}
