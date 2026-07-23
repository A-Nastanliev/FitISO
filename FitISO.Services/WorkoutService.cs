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

        public async Task<Workout> CreateAsync(string name, List<WorkoutExercise> workoutExercises)
        {
            var workout = new Workout
            {
                Name = name,
                StartTime = null,
                EndTime = null,
                WorkoutExercises = workoutExercises ?? new List<WorkoutExercise>()
            };

            using var _context = _contextFactory.CreateDbContext();
            _context.Workouts.Add(workout);
            await _context.SaveChangesAsync();

            var savedWorkout = await _context.Workouts
                .Include(w => w.WorkoutExercises)
                    .ThenInclude(we => we.Exercise)
                .Include(w => w.WorkoutExercises)
                    .ThenInclude(we => we.Sets)
                .AsSplitQuery()
                .FirstOrDefaultAsync(w => w.Id == workout.Id);

            return savedWorkout!;
        }

        public async Task<Workout> StartFromTemplateAsync(int templateId)
        {
            using var _context = _contextFactory.CreateDbContext();

            var activeWorkout = await _context.Workouts
                .Where(w => w.StartTime != null && w.EndTime == null)
                .Select(w => new { w.Name })
                .FirstOrDefaultAsync();

            if (activeWorkout != null)
                throw new InvalidOperationException($"Workout {activeWorkout.Name} is already active. Finish it before starting another.");

            var template = await _context.Workouts
                .Include(w => w.WorkoutExercises)
                    .ThenInclude(we => we.Sets)
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync(w => w.Id == templateId);

            if (template == null)
                throw new KeyNotFoundException($"Workout {templateId} was not found.");

            if (template.StartTime != null)
                throw new InvalidOperationException($"Workout {templateId} is not a template.");

            var newWorkout = new Workout
            {
                Name = template.Name,
                StartTime = DateTime.UtcNow,
                EndTime = null,
                WorkoutExercises = template.WorkoutExercises.Select(we => new WorkoutExercise
                {
                    ExerciseId = we.ExerciseId,
                    Sets = we.Sets.Select(s => new Set
                    {
                        Weight = s.Weight,
                        Reps = s.Reps
                    }).ToList()
                }).ToList()
            };

            _context.Workouts.Add(newWorkout);
            await _context.SaveChangesAsync();

            var savedWorkout = await _context.Workouts
                .Include(w => w.WorkoutExercises)
                    .ThenInclude(we => we.Exercise)
                .Include(w => w.WorkoutExercises)
                    .ThenInclude(we => we.Sets)
                .AsSplitQuery()
                .FirstOrDefaultAsync(w => w.Id == newWorkout.Id);

            var exercises = savedWorkout!.WorkoutExercises.Select(we => we.Exercise).ToList();
            await PopulateExerciseHistoryAsync(_context, exercises, excludeWorkoutId: newWorkout.Id);

            return savedWorkout!;
        }

        public async Task<Workout> GetActiveWorkoutAsync()
        {
            using var _context = _contextFactory.CreateDbContext();
            var workout = await _context.Workouts
                .Include(w => w.WorkoutExercises)
                    .ThenInclude(we => we.Sets)
                .Include(w => w.WorkoutExercises)
                    .ThenInclude(we => we.Exercise)
                .FirstOrDefaultAsync(w => w.StartTime != null && w.EndTime == null);

            if (workout != null)
            {
                var exercises = workout.WorkoutExercises.Select(we => we.Exercise).ToList();
                await PopulateExerciseHistoryAsync(_context, exercises, excludeWorkoutId: workout.Id);
            }

            return workout;
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

        public async Task<Workout> UpdateAsync(int id, string name, List<WorkoutExercise> incomingExercises)
        {
            using var _context = _contextFactory.CreateDbContext();

            var workout = await _context.Workouts
                .Include(w => w.WorkoutExercises)
                    .ThenInclude(we => we.Sets)
                .Include(w => w.WorkoutExercises)
                    .ThenInclude(e => e.Exercise)
                .AsSplitQuery()
                .FirstOrDefaultAsync(w => w.Id == id);

            if (workout == null)
                throw new KeyNotFoundException($"Workout {id} was not found.");

            workout.Name = name;

            var existingExercisesById = workout.WorkoutExercises.ToDictionary(we => we.Id);
            var incomingExerciseIds = incomingExercises.Where(we => we.Id != 0).Select(we => we.Id).ToHashSet();

            foreach (var existing in existingExercisesById.Values.ToList())
            {
                if (!incomingExerciseIds.Contains(existing.Id))
                    workout.WorkoutExercises.Remove(existing);
            }

            foreach (var incoming in incomingExercises)
            {
                if (incoming.Id != 0 && existingExercisesById.TryGetValue(incoming.Id, out var existingExercise))
                {
                    if (existingExercise.ExerciseId != incoming.ExerciseId)
                    {
                        existingExercise.ExerciseId = incoming.ExerciseId;
                        existingExercise.Exercise = await _context.Exercises.FindAsync(incoming.ExerciseId);
                    }

                    var existingSetsById = existingExercise.Sets.ToDictionary(s => s.Id);
                    var incomingSetIds = incoming.Sets.Where(s => s.Id != 0).Select(s => s.Id).ToHashSet();

                    foreach (var existingSet in existingSetsById.Values.ToList())
                    {
                        if (!incomingSetIds.Contains(existingSet.Id))
                            existingExercise.Sets.Remove(existingSet);
                    }

                    foreach (var incomingSet in incoming.Sets)
                    {
                        if (incomingSet.Id != 0 && existingSetsById.TryGetValue(incomingSet.Id, out var existingSet))
                        {
                            existingSet.Weight = incomingSet.Weight;
                            existingSet.Reps = incomingSet.Reps;
                        }
                        else
                        {
                            existingExercise.Sets.Add(new Set { Weight = incomingSet.Weight, Reps = incomingSet.Reps });
                        }
                    }
                }
                else
                {
                    var exercise = await _context.Exercises.FindAsync(incoming.ExerciseId);

                    workout.WorkoutExercises.Add(new WorkoutExercise
                    {
                        ExerciseId = incoming.ExerciseId,
                        Exercise = exercise,
                        Sets = incoming.Sets.Select(s => new Set { Weight = s.Weight, Reps = s.Reps }).ToList()
                    });
                }
            }

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

        private async Task PopulateExerciseHistoryAsync( FitDbContext _context, IEnumerable<Exercise> exercises, int? excludeWorkoutId = null)
        {
            var exerciseIds = exercises.Select(e => e.Id).Distinct().ToList();
            if (exerciseIds.Count == 0)
                return;

            var bestSets = await _context.Sets
                .AsNoTracking()
                .Where(s => exerciseIds.Contains(s.WorkoutExercise.ExerciseId)
                         && s.WorkoutExercise.Workout.EndTime != null
                         && (excludeWorkoutId == null || s.WorkoutExercise.WorkoutId != excludeWorkoutId))
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
                .Where(we => exerciseIds.Contains(we.ExerciseId)
                          && we.Workout.EndTime != null
                          && (excludeWorkoutId == null || we.WorkoutId != excludeWorkoutId))
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
        }
    }
}
