using FitISO.Data;
using FitISO.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FitISO.Services
{
    public class ExerciseService
    {
        const int MinNameLength = 4;
        const int MaxNameLength = 100;

        readonly FitDbContext _context;

        public ExerciseService(FitDbContext context)
        {
            _context = context;
        }

        public async Task<Exercise> CreateAsync(string name)
        {
            name = ValidateName(name);

            var exists = await _context.Exercises.AnyAsync(e => e.Name == name);
            if (exists)
                throw new InvalidOperationException($"An exercise named '{name}' already exists.");

            var exercise = new Exercise { Name = name };
            _context.Exercises.Add(exercise);
            await _context.SaveChangesAsync();
            return exercise;
        }

        public async Task<List<Exercise>> GetNextAsync(int pageSize, string? cursor = null)
        {
            IQueryable<Exercise> query = _context.Exercises.AsNoTracking();

            if (!string.IsNullOrEmpty(cursor))
                query = query.Where(e => e.Name.CompareTo(cursor) > 0);

            return await query
                .OrderBy(e => e.Name)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Exercise> UpdateNameAsync(int id, string name)
        {
            name = ValidateName(name);

            var exercise = await _context.Exercises.FindAsync(id);
            if (exercise == null)
                throw new KeyNotFoundException($"Exercise {id} was not found.");

            var exists = await _context.Exercises.AnyAsync(e => e.Id != id && e.Name == name);
            if (exists)
                throw new InvalidOperationException($"An exercise named '{name}' already exists.");

            exercise.Name = name;
            await _context.SaveChangesAsync();
            return exercise;
        }

        public async Task DeleteAsync(int id)
        {
            var exercise = await _context.Exercises.FindAsync(id);
            if (exercise == null)
                throw new KeyNotFoundException($"Exercise {id} was not found.");

            _context.Exercises.Remove(exercise);
            await _context.SaveChangesAsync();
        }

        public async Task<Set> GetBestSetAsync(int exerciseId)
        {
            return await _context.Sets
                .AsNoTracking()
                .Where(s => s.WorkoutExercise.ExerciseId == exerciseId)
                .OrderByDescending(s => s.Weight)
                .ThenByDescending(s => s.Reps)
                .FirstOrDefaultAsync();
        }

        public async Task<(DateTime? Date, List<Set> Sets)> GetLastSetsAsync(int exerciseId)
        {
            var lastWorkoutExercise = await _context.WorkoutExercises
                .AsNoTracking()
                .Include(we => we.Workout)
                .Where(we => we.ExerciseId == exerciseId && we.Workout.StartTime != null)
                .OrderByDescending(we => we.Workout.StartTime)
                .FirstOrDefaultAsync();

            if (lastWorkoutExercise == null)
                return (null, new List<Set>());

            var sets = await _context.Sets
                .AsNoTracking()
                .Where(s => s.WorkoutExerciseId == lastWorkoutExercise.Id)
                .ToListAsync();

            return (lastWorkoutExercise.Workout.StartTime, sets);
        }


        private static string ValidateName(string name)
        {
            name = name?.Trim() ?? string.Empty;

            if (name.Length < MinNameLength || name.Length > MaxNameLength)
                throw new ArgumentException($"Exercise name must be between {MinNameLength} and {MaxNameLength} characters.", nameof(name));

            return name;
        }
    }
}