using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitISO.Data.Models
{
    public class WorkoutExercise
    {
        [Key]
        public int Id { get; set; }  

        public int WorkoutId { get; set; }
        [ForeignKey(nameof(WorkoutId))]
        public Workout Workout { get; set; }

        public int ExerciseId { get; set; }
        [ForeignKey(nameof(ExerciseId))]
        public Exercise Exercise { get; set; }

        [MaxLength(100)]
        public string Note { get; set; }

        public List<Set> Sets { get; set; }
    }
}
