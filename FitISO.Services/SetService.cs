using FitISO.Data;
using FitISO.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FitISO.Services
{
    public class SetService
    {
        readonly IDbContextFactory<FitDbContext> _contextFactory;

        public SetService(IDbContextFactory<FitDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<Set> CreateAsync(int workoutExerciseId, double? weight, double? reps)
        {
            ValidateWeightAndReps(weight, reps);

            var set = new Set
            {
                WorkoutExerciseId = workoutExerciseId,
                Weight = weight,
                Reps = reps
            };

            using var _context = _contextFactory.CreateDbContext();
            _context.Sets.Add(set);
            await _context.SaveChangesAsync();
            return set;
        }

        public async Task<Set> UpdateAsync(int id, double? weight = null, double? reps = null)
        {
            using var _context = _contextFactory.CreateDbContext();
            var set = await _context.Sets.FindAsync(id);
            if (set == null)
                throw new KeyNotFoundException($"Set {id} was not found.");

            var resultingWeight = weight ?? set.Weight;
            var resultingReps = reps ?? set.Reps;

            ValidateWeightAndReps(resultingWeight, resultingReps);

            set.Weight = resultingWeight;
            set.Reps = resultingReps;

            await _context.SaveChangesAsync();
            return set;
        }

        public async Task DeleteAsync(int id)
        {
            using var _context = _contextFactory.CreateDbContext();
            var set = await _context.Sets.FindAsync(id);
            if (set == null)
                throw new KeyNotFoundException($"Set {id} was not found.");

            _context.Sets.Remove(set);
            await _context.SaveChangesAsync();
        }

        private static void ValidateWeightAndReps(double? weight, double? reps)
        {
            if (weight < 0)
                throw new ArgumentException("Weight cannot be negative.", nameof(weight));

            if (reps < 0)
                throw new ArgumentException("Reps cannot be negative.", nameof(reps));
        }
    }
}