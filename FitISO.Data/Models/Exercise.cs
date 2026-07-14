using System.ComponentModel.DataAnnotations;

namespace FitISO.Data.Models
{
    public class Exercise
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Length(4,100)]
        public string Name { get; set; }

        public IEnumerable<WorkoutExercise> WorkoutExercises { get; set; }
    }
}
