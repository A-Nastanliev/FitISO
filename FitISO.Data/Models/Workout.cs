using System.ComponentModel.DataAnnotations;

namespace FitISO.Data.Models
{
    public class Workout
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public IEnumerable<WorkoutExercise> WorkoutExercises { get; set; }
    }
}
