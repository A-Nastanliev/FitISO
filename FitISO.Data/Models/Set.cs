using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace FitISO.Data.Models
{
    public class Set
    {
        [Key]
        public int Id { get; set; }
        public int WorkoutExerciseId { get; set; }
        [ForeignKey(nameof(WorkoutExerciseId))]
        public WorkoutExercise WorkoutExercise { get; set; }

        public double Weight { get; set; }
        public double Reps { get; set; }

    }
}
