using FitISO.Data;
using FitISO.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FitISO.Services
{
    public class WorkoutExerciseService
    {
        private const int MaxNoteLength = 100;

        private readonly FitDbContext _context;

        public WorkoutExerciseService(FitDbContext context)
        {
            _context = context;
        }

        public async Task<WorkoutExercise> CreateAsync(int workoutId, int exerciseId, string? note = null)
        {
            ValidateNote(note);

            var workoutExercise = new WorkoutExercise
            {
                WorkoutId = workoutId,
                ExerciseId = exerciseId,
                Note = note
            };

            _context.WorkoutExercises.Add(workoutExercise);
            await _context.SaveChangesAsync();
            return workoutExercise;
        }

        public async Task<WorkoutExercise> UpdateAsync(int id, int? exerciseId = null, string? note = null)
        {
            ValidateNote(note);

            var workoutExercise = await _context.WorkoutExercises.FindAsync(id);
            if (workoutExercise == null)
                throw new KeyNotFoundException($"WorkoutExercise {id} was not found.");

            if (exerciseId.HasValue)
                workoutExercise.ExerciseId = exerciseId.Value;

            if (note != null)
                workoutExercise.Note = note;

            await _context.SaveChangesAsync();
            return workoutExercise;
        }

        public async Task DeleteAsync(int id)
        {
            var workoutExercise = await _context.WorkoutExercises.FindAsync(id);
            if (workoutExercise == null)
                throw new KeyNotFoundException($"WorkoutExercise {id} was not found.");

            _context.WorkoutExercises.Remove(workoutExercise);
            await _context.SaveChangesAsync();
        }

        private static void ValidateNote(string? note)
        {
            if (note != null && note.Length > MaxNoteLength)
                throw new ArgumentException($"Note cannot exceed {MaxNoteLength} characters.", nameof(note));
        }
    }
}