using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FitISO.Maui.Models
{
    public partial class Workout : ObservableObject
    {
        [ObservableProperty]
        int id;
        [ObservableProperty]
        string name;
        [ObservableProperty]
        DateTime? startTime;
        [ObservableProperty]
        DateTime? endTime;
        [ObservableProperty]
        ObservableCollection<WorkoutExercise> workoutExercises = new();

        public Workout() { }
        
        public Workout(FitISO.Data.Models.Workout workout)
        {
            Id = workout.Id;
            Name = workout.Name;
            StartTime = workout.StartTime;
            EndTime = workout.EndTime;
            foreach(var we in workout.WorkoutExercises)
            {
                WorkoutExercises.Add(new WorkoutExercise(we));
            }
        }
    }
}
