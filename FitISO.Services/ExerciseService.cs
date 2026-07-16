using FitISO.Data;
using FitISO.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FitISO.Services
{
    public class ExerciseService
    {
        const int MinNameLength = 4;
        const int MaxNameLength = 100;

        readonly IDbContextFactory<FitDbContext> _contextFactory;

        public ExerciseService(IDbContextFactory<FitDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<Exercise> CreateAsync(string name)
        {
            name = ValidateName(name);

            using var _context = _contextFactory.CreateDbContext();
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
            using var _context = _contextFactory.CreateDbContext();
            IQueryable<Exercise> query = _context.Exercises.AsNoTracking();

            if (!string.IsNullOrEmpty(cursor))
                query = query.Where(e => e.Name.CompareTo(cursor) > 0);

            var exercises = await query
                .OrderBy(e => e.Name)
                .Take(pageSize)
                .ToListAsync();

            var exerciseIds = exercises.Select(e => e.Id).ToList();
            if (exerciseIds.Count == 0)
                return exercises;

            var bestSets = await _context.Sets
                .AsNoTracking()
                .Where(s => exerciseIds.Contains(s.WorkoutExercise.ExerciseId))
                .GroupBy(s => s.WorkoutExercise.ExerciseId)
                .Select(g => new
                {
                    ExerciseId = g.Key,
                    Set = g.OrderByDescending(s => s.Weight)
                           .ThenByDescending(s => s.Reps)
                           .First()
                })
                .ToListAsync();

            var bestSetByExercise = bestSets.ToDictionary(x => x.ExerciseId, x => x.Set);

            var lastWorkoutExercises = await _context.WorkoutExercises
                .AsNoTracking()
                .Include(we => we.Workout)
                .Where(we => exerciseIds.Contains(we.ExerciseId) && we.Workout.StartTime != null)
                .GroupBy(we => we.ExerciseId)
                .Select(g => g.OrderByDescending(we => we.Workout.StartTime).First())
                .ToListAsync();

            var lastWeIds = lastWorkoutExercises.Select(we => we.Id).ToList();

            var lastSets = await _context.Sets
                .AsNoTracking()
                .Where(s => lastWeIds.Contains(s.WorkoutExerciseId))
                .ToListAsync();

            var lastSetsByWeId = lastSets
                .GroupBy(s => s.WorkoutExerciseId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var lastInfoByExercise = lastWorkoutExercises.ToDictionary(
                we => we.ExerciseId,
                we => (we.Workout.StartTime, Sets: lastSetsByWeId.GetValueOrDefault(we.Id, new List<Set>())));

            foreach (var exercise in exercises)
            {
                exercise.BestSet = bestSetByExercise.GetValueOrDefault(exercise.Id);

                if (lastInfoByExercise.TryGetValue(exercise.Id, out var lastInfo))
                {
                    exercise.LastSetsDate = lastInfo.StartTime;
                    exercise.LastSets = lastInfo.Sets;
                }
            }

            return exercises;
        }

        public async Task<Exercise> UpdateNameAsync(int id, string name)
        {
            name = ValidateName(name);

            using var _context = _contextFactory.CreateDbContext();
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
            using var _context = _contextFactory.CreateDbContext();
            var exercise = await _context.Exercises.FindAsync(id);
            if (exercise == null)
                throw new KeyNotFoundException($"Exercise {id} was not found.");

            _context.Exercises.Remove(exercise);
            await _context.SaveChangesAsync();
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