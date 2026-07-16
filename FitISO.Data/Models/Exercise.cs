using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [NotMapped]
        public Set? BestSet { get; set; }

        [NotMapped]
        public DateTime? LastSetsDate { get; set; }

        [NotMapped]
        public List<Set> LastSets { get; set; } = new();
    }
}
