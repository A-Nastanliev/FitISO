using FitISO.Data;
using FitISO.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FitISO.Services
{
    public class WorkoutService
    {
        readonly IDbContextFactory<FitDbContext> _contextFactory;

        public WorkoutService(IDbContextFactory<FitDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<Workout> CreateAsync(string name, bool isTemplate)
        {
            if (!isTemplate)
            {
                var activeWorkout = await GetActiveWorkoutAsync();
                if (activeWorkout != null)
                    throw new InvalidOperationException($"Workout {activeWorkout.Name} is already active. Finish it before starting another.");
            }

            var workout = new Workout
            {
                Name = name,
                StartTime = isTemplate ? null : DateTime.UtcNow,
                EndTime = null
            };

            using var _context = _contextFactory.CreateDbContext();
            _context.Workouts.Add(workout);
            await _context.SaveChangesAsync();
            return workout;
        }

        public async Task<Workout> GetActiveWorkoutAsync()
        {
            using var _context = _contextFactory.CreateDbContext();
            return await _context.Workouts
                .Include(w => w.WorkoutExercises)
                    .ThenInclude(we => we.Sets)
                .Include(w => w.WorkoutExercises)
                    .ThenInclude(we => we.Exercise)
                .FirstOrDefaultAsync(w => w.StartTime != null && w.EndTime == null);
        }

        private async Task<List<Workout>> GetNextAsync(bool templatesOnly, int pageSize, int? cursor = null)
        {
            using var _context = _contextFactory.CreateDbContext();
            IQueryable<Workout> query = _context.Workouts
                .AsNoTracking()
                .Include(w => w.WorkoutExercises)
                    .ThenInclude(we => we.Sets)
                .Include(w => w.WorkoutExercises)
                    .ThenInclude(we => we.Exercise);

            query = templatesOnly
                ? query.Where(w => w.StartTime == null && w.EndTime == null)
                : query.Where(w => w.StartTime != null);

            if (cursor.HasValue)
                query = query.Where(w => w.Id > cursor.Value);

            return await query
                .OrderBy(w => w.Id)
                .Take(pageSize)
                .ToListAsync();
        }
        public Task<List<Workout>> GetWorkoutsAsync(int pageSize,int? cursor = null)
            => GetNextAsync(templatesOnly: false, pageSize, cursor);

        public Task<List<Workout>> GetTemplatesAsync(int pageSize, int? cursor = null)
             => GetNextAsync(templatesOnly: true, pageSize, cursor);

        public async Task<Workout> UpdateNameAsync(int id, string name)
        {
            using var _context = _contextFactory.CreateDbContext();
            var workout = await _context.Workouts.FindAsync(id);
            if (workout == null)
                throw new KeyNotFoundException($"Workout {id} was not found.");

            workout.Name = name;

            await _context.SaveChangesAsync();
            return workout;
        }

        public async Task<Workout> EndWorkoutAsync(int id)
        {
            using var _context = _contextFactory.CreateDbContext();
            var workout = await _context.Workouts.FindAsync(id);
            if (workout == null)
                throw new KeyNotFoundException($"Workout {id} was not found.");

            if (workout.StartTime == null)
                throw new InvalidOperationException("Cannot end a workout that never started (this is a template).");

            if (workout.EndTime != null)
                throw new InvalidOperationException($"Workout {id} has already ended.");

            workout.EndTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return workout;
        }


        public async Task DeleteAsync(int id)
        {
            using var _context = _contextFactory.CreateDbContext();
            var workout = await _context.Workouts.FindAsync(id);
            if (workout == null)
                throw new KeyNotFoundException($"Workout {id} was not found.");

            _context.Workouts.Remove(workout);
            await _context.SaveChangesAsync();
        }
    }
}
