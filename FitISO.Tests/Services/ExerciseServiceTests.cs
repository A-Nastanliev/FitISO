using FitISO.Data;
using FitISO.Data.Models;
using FitISO.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitISO.Tests.Services
{
    [TestFixture]
    public class ExerciseServiceTests
    {
        private SqliteConnection _connection;
        private FitDbContext _context;
        private ExerciseService _service;
        private TestDbContextFactory _contextFactory;

        [SetUp]
        public void SetUp()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<FitDbContext>()
                .UseSqlite(_connection)
                .Options;

            _contextFactory = new TestDbContextFactory(options);
            _context = _contextFactory.CreateDbContext();
            _context.Database.EnsureCreated();

            _service = new ExerciseService(_contextFactory);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        private async Task<Exercise> SeedExerciseAsync(string name)
        {
            var exercise = new Exercise { Name = name };
            _context.Exercises.Add(exercise);
            await _context.SaveChangesAsync();
            return exercise;
        }

        private async Task<WorkoutExercise> SeedWorkoutExerciseAsync(int exerciseId, DateTime? startTime)
        {
            var endTime = startTime.HasValue ? startTime.Value.AddMinutes(30) : (DateTime?)null;
            var workout = new Workout { Name = "Workout", StartTime = startTime, EndTime = endTime };
            _context.Workouts.Add(workout);
            await _context.SaveChangesAsync();

            var workoutExercise = new WorkoutExercise
            {
                WorkoutId = workout.Id,
                ExerciseId = exerciseId
            };
            _context.WorkoutExercises.Add(workoutExercise);
            await _context.SaveChangesAsync();

            return workoutExercise;
        }

        private async Task<Set> SeedSetAsync(int workoutExerciseId, double weight, double reps)
        {
            var set = new Set
            {
                WorkoutExerciseId = workoutExerciseId,
                Weight = weight,
                Reps = reps
            };
            _context.Sets.Add(set);
            await _context.SaveChangesAsync();
            return set;
        }

        [Test]
        public async Task CreateAsync_WithValidName_PersistsAndReturnsExercise()
        {
            var result = await _service.CreateAsync("Bench Press");

            Assert.That(result.Id, Is.GreaterThan(0));
            Assert.That(result.Name, Is.EqualTo("Bench Press"));

            var stored = await _context.Exercises.FindAsync(result.Id);
            Assert.That(stored, Is.Not.Null);
        }

        [Test]
        public async Task CreateAsync_WithSurroundingWhitespace_TrimsName()
        {
            var result = await _service.CreateAsync("  Squat  ");

            Assert.That(result.Name, Is.EqualTo("Squat"));
        }

        [Test]
        public async Task CreateAsync_WithDuplicateName_ThrowsInvalidOperationException()
        {
            await _service.CreateAsync("Deadlift");

            Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateAsync("Deadlift"));
        }

        [Test]
        public void CreateAsync_WithNameTooShort_ThrowsArgumentException()
        {
            var ex = Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync("Ab"));

            Assert.That(ex.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void CreateAsync_WithNameTooLong_ThrowsArgumentException()
        {
            var tooLong = new string('a', 101);

            Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(tooLong));
        }

        [Test]
        public void CreateAsync_WithNullName_ThrowsArgumentException()
        {
            Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(null));
        }

        [Test]
        public async Task GetNextAsync_ReturnsExercisesOrderedByName()
        {
            await SeedExerciseAsync("Squat");
            await SeedExerciseAsync("Bench Press");
            await SeedExerciseAsync("Deadlift");

            var result = await _service.GetNextAsync(pageSize: 10);

            Assert.That(result.Select(e => e.Name), Is.EqualTo(new[] { "Bench Press", "Deadlift", "Squat" }));
        }

        [Test]
        public async Task GetNextAsync_RespectsPageSize()
        {
            await SeedExerciseAsync("Squat");
            await SeedExerciseAsync("Bench Press");
            await SeedExerciseAsync("Deadlift");

            var result = await _service.GetNextAsync(pageSize: 2);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Select(e => e.Name), Is.EqualTo(new[] { "Bench Press", "Deadlift" }));
        }

        [Test]
        public async Task GetNextAsync_WithCursor_ReturnsItemsAfterCursorAlphabetically()
        {
            await SeedExerciseAsync("Squat");
            await SeedExerciseAsync("Bench Press");
            await SeedExerciseAsync("Deadlift");

            var result = await _service.GetNextAsync(pageSize: 10, cursor: "Deadlift");

            Assert.That(result.Select(e => e.Name), Is.EqualTo(new[] { "Squat" }));
        }

        [Test]
        public async Task GetNextAsync_WithNoMatchingExercises_ReturnsEmptyList()
        {
            var result = await _service.GetNextAsync(pageSize: 10);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetNextAsync_PopulatesBestSet_ForEachExercise()
        {
            var exercise = await SeedExerciseAsync("Squat");
            var workoutExercise = await SeedWorkoutExerciseAsync(exercise.Id, DateTime.UtcNow);

            await SeedSetAsync(workoutExercise.Id, weight: 100, reps: 5);
            var best = await SeedSetAsync(workoutExercise.Id, weight: 150, reps: 3);
            await SeedSetAsync(workoutExercise.Id, weight: 120, reps: 8);

            var result = await _service.GetNextAsync(pageSize: 10);

            var returned = result.Single(e => e.Id == exercise.Id);
            Assert.That(returned.BestSet, Is.Not.Null);
            Assert.That(returned.BestSet.Id, Is.EqualTo(best.Id));
        }

        [Test]
        public async Task GetNextAsync_WithTiedBestWeight_BreaksTieByHighestReps()
        {
            var exercise = await SeedExerciseAsync("Squat");
            var workoutExercise = await SeedWorkoutExerciseAsync(exercise.Id, DateTime.UtcNow);

            await SeedSetAsync(workoutExercise.Id, weight: 100, reps: 5);
            var best = await SeedSetAsync(workoutExercise.Id, weight: 100, reps: 8);

            var result = await _service.GetNextAsync(pageSize: 10);

            var returned = result.Single(e => e.Id == exercise.Id);
            Assert.That(returned.BestSet.Id, Is.EqualTo(best.Id));
        }

        [Test]
        public async Task GetNextAsync_WithNoSets_BestSetIsNullAndLastSetsIsEmpty()
        {
            await SeedExerciseAsync("Squat");

            var result = await _service.GetNextAsync(pageSize: 10);

            var returned = result.Single();
            Assert.That(returned.BestSet, Is.Null);
            Assert.That(returned.LastSetsDate, Is.Null);
            Assert.That(returned.LastSets, Is.Empty);
        }

        [Test]
        public async Task GetNextAsync_PopulatesLastSets_FromMostRecentWorkout()
        {
            var exercise = await SeedExerciseAsync("Squat");

            var older = await SeedWorkoutExerciseAsync(exercise.Id, DateTime.UtcNow.AddDays(-7));
            var newer = await SeedWorkoutExerciseAsync(exercise.Id, DateTime.UtcNow.AddDays(-1));

            await SeedSetAsync(older.Id, weight: 90, reps: 10);
            var expectedSet = await SeedSetAsync(newer.Id, weight: 100, reps: 8);

            var result = await _service.GetNextAsync(pageSize: 10);

            var returned = result.Single(e => e.Id == exercise.Id);
            Assert.That(returned.LastSetsDate, Is.EqualTo(newer.Workout.StartTime));
            Assert.That(returned.LastSets.Select(s => s.Id), Is.EqualTo(new[] { expectedSet.Id }));
        }

        [Test]
        public async Task GetNextAsync_LastSets_ExcludesWorkoutsWithNullStartTime()
        {
            var exercise = await SeedExerciseAsync("Squat");

            var withoutStart = await SeedWorkoutExerciseAsync(exercise.Id, startTime: null);
            var withStart = await SeedWorkoutExerciseAsync(exercise.Id, DateTime.UtcNow.AddDays(-1));

            await SeedSetAsync(withoutStart.Id, weight: 90, reps: 10);
            var expectedSet = await SeedSetAsync(withStart.Id, weight: 100, reps: 8);

            var result = await _service.GetNextAsync(pageSize: 10);

            var returned = result.Single(e => e.Id == exercise.Id);
            Assert.That(returned.LastSets.Select(s => s.Id), Is.EqualTo(new[] { expectedSet.Id }));
        }

        [Test]
        public async Task GetNextAsync_WithMultipleExercises_EachGetsItsOwnBestSetAndLastSets()
        {
            var squat = await SeedExerciseAsync("Squat");
            var bench = await SeedExerciseAsync("Bench Press");

            var squatWorkoutExercise = await SeedWorkoutExerciseAsync(squat.Id, DateTime.UtcNow);
            var benchWorkoutExercise = await SeedWorkoutExerciseAsync(bench.Id, DateTime.UtcNow);

            var squatBest = await SeedSetAsync(squatWorkoutExercise.Id, weight: 100, reps: 5);
            var benchBest = await SeedSetAsync(benchWorkoutExercise.Id, weight: 200, reps: 5);

            var result = await _service.GetNextAsync(pageSize: 10);

            var returnedSquat = result.Single(e => e.Id == squat.Id);
            var returnedBench = result.Single(e => e.Id == bench.Id);

            Assert.That(returnedSquat.BestSet.Id, Is.EqualTo(squatBest.Id));
            Assert.That(returnedBench.BestSet.Id, Is.EqualTo(benchBest.Id));
        }

        [Test]
        public async Task GetNextAsync_OnlyPopulatesDataForExercisesInThePage()
        {
            var included = await SeedExerciseAsync("Bench Press");
            var excluded = await SeedExerciseAsync("Squat");

            var includedWorkoutExercise = await SeedWorkoutExerciseAsync(included.Id, DateTime.UtcNow);
            var excludedWorkoutExercise = await SeedWorkoutExerciseAsync(excluded.Id, DateTime.UtcNow);

            await SeedSetAsync(includedWorkoutExercise.Id, weight: 100, reps: 5);
            await SeedSetAsync(excludedWorkoutExercise.Id, weight: 999, reps: 5);

            var result = await _service.GetNextAsync(pageSize: 1);

            Assert.That(result.Select(e => e.Name), Is.EqualTo(new[] { "Bench Press" }));
            Assert.That(result.Single().BestSet, Is.Not.Null);
        }

        [Test]
        public async Task GetNextAsync_BestSetIgnoresSetsFromOtherExercises()
        {
            var squat = await SeedExerciseAsync("Squat");
            var bench = await SeedExerciseAsync("Bench Press");

            var squatWorkoutExercise = await SeedWorkoutExerciseAsync(squat.Id, DateTime.UtcNow);
            var benchWorkoutExercise = await SeedWorkoutExerciseAsync(bench.Id, DateTime.UtcNow);

            var squatBest = await SeedSetAsync(squatWorkoutExercise.Id, weight: 100, reps: 5);
            await SeedSetAsync(benchWorkoutExercise.Id, weight: 999, reps: 5);

            var result = await _service.GetNextAsync(pageSize: 10);

            var returnedSquat = result.Single(e => e.Id == squat.Id);
            Assert.That(returnedSquat.BestSet.Id, Is.EqualTo(squatBest.Id));
        }

        [Test]
        public async Task UpdateNameAsync_WithValidName_UpdatesAndReturnsExercise()
        {
            var exercise = await SeedExerciseAsync("Squat");

            var updated = await _service.UpdateNameAsync(exercise.Id, "Front Squat");

            Assert.That(updated.Name, Is.EqualTo("Front Squat"));
        }

        [Test]
        public async Task UpdateNameAsync_WithSurroundingWhitespace_TrimsName()
        {
            var exercise = await SeedExerciseAsync("Squat");

            var updated = await _service.UpdateNameAsync(exercise.Id, "  Front Squat  ");

            Assert.That(updated.Name, Is.EqualTo("Front Squat"));
        }

        [Test]
        public async Task UpdateNameAsync_WithSameNameAsSelf_DoesNotThrow()
        {
            var exercise = await SeedExerciseAsync("Squat");

            var updated = await _service.UpdateNameAsync(exercise.Id, "Squat");

            Assert.That(updated.Name, Is.EqualTo("Squat"));
        }

        [Test]
        public async Task UpdateNameAsync_WithNameUsedByAnotherExercise_ThrowsInvalidOperationException()
        {
            await SeedExerciseAsync("Squat");
            var other = await SeedExerciseAsync("Deadlift");

            Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateNameAsync(other.Id, "Squat"));
        }

        [Test]
        public void UpdateNameAsync_WithNonExistentId_ThrowsKeyNotFoundException()
        {
            Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.UpdateNameAsync(9999, "Squat"));
        }

        [Test]
        public async Task UpdateNameAsync_WithInvalidName_ThrowsArgumentException()
        {
            var exercise = await SeedExerciseAsync("Squat");

            Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateNameAsync(exercise.Id, "Ab"));
        }

        [Test]
        public async Task DeleteAsync_WithExistingId_RemovesExercise()
        {
            var exercise = await SeedExerciseAsync("Squat");

            await _service.DeleteAsync(exercise.Id);

            var stored = await _context.Exercises
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == exercise.Id);
            Assert.That(stored, Is.Null);
        }

        [Test]
        public void DeleteAsync_WithNonExistentId_ThrowsKeyNotFoundException()
        {
            Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.DeleteAsync(9999));
        }

        [Test]
        public async Task IsDeletable_WithNoWorkoutExercises_ReturnsTrue()
        {
            var exercise = await SeedExerciseAsync("Squat");

            var result = await _service.IsDeletable(exercise.Id);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task IsDeletable_WithExistingWorkoutExercise_ReturnsFalse()
        {
            var exercise = await SeedExerciseAsync("Squat");
            await SeedWorkoutExerciseAsync(exercise.Id, DateTime.UtcNow);

            var result = await _service.IsDeletable(exercise.Id);

            Assert.That(result, Is.False);
        }

    }
}