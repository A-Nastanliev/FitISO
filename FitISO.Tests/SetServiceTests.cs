using System;
using System.Threading.Tasks;
using FitISO.Data;
using FitISO.Data.Models;
using FitISO.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace FitISO.Tests.Services
{
    [TestFixture]
    public class SetServiceTests
    {
        private SqliteConnection _connection;
        private FitDbContext _context;
        private SetService _service;
        private int _workoutExerciseId;
        private TestDbContextFactory _contextFactory;

        [SetUp]
        public async Task SetUp()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<FitDbContext>()
                .UseSqlite(_connection)
                .Options;

            _contextFactory = new TestDbContextFactory(options);
            _context = _contextFactory.CreateDbContext();
            _context.Database.EnsureCreated();

            _service = new SetService(_contextFactory);

            var workout = new Workout { Name = "Test Workout" };
            var exercise = new Exercise { Name = "Bench Press" };
            _context.AddRange(workout, exercise);
            await _context.SaveChangesAsync();

            var workoutExercise = new WorkoutExercise
            {
                WorkoutId = workout.Id,
                ExerciseId = exercise.Id
            };
            _context.WorkoutExercises.Add(workoutExercise);
            await _context.SaveChangesAsync();

            _workoutExerciseId = workoutExercise.Id;
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [Test]
        public async Task CreateAsync_WithValidData_PersistsAndReturnsSet()
        {
            var result = await _service.CreateAsync(workoutExerciseId: _workoutExerciseId, weight: 100, reps: 10);

            Assert.That(result.Id, Is.GreaterThan(0));
            Assert.That(result.WorkoutExerciseId, Is.EqualTo(_workoutExerciseId));
            Assert.That(result.Weight, Is.EqualTo(100));
            Assert.That(result.Reps, Is.EqualTo(10));

            var stored = await _context.Sets.FindAsync(result.Id);
            Assert.That(stored, Is.Not.Null);
        }

        [Test]
        public async Task CreateAsync_WithZeroWeightAndReps_IsAllowed()
        {
            var result = await _service.CreateAsync(_workoutExerciseId, 0, 0);

            Assert.That(result.Weight, Is.EqualTo(0));
            Assert.That(result.Reps, Is.EqualTo(0));
        }

        [Test]
        public void CreateAsync_WithNegativeWeight_ThrowsArgumentException()
        {
            var ex = Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(_workoutExerciseId, weight: -1, reps: 10));

            Assert.That(ex.ParamName, Is.EqualTo("weight"));
        }

        [Test]
        public void CreateAsync_WithNegativeReps_ThrowsArgumentException()
        {
            var ex = Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(_workoutExerciseId, weight: 10, reps: -1));

            Assert.That(ex.ParamName, Is.EqualTo("reps"));
        }


        [Test]
        public async Task UpdateAsync_WithBothValuesProvided_UpdatesBoth()
        {
            var created = await _service.CreateAsync(_workoutExerciseId, 100, 10);

            var updated = await _service.UpdateAsync(created.Id, weight: 120, reps: 8);

            Assert.That(updated.Weight, Is.EqualTo(120));
            Assert.That(updated.Reps, Is.EqualTo(8));
        }

        [Test]
        public async Task UpdateAsync_WithOnlyWeightProvided_KeepsExistingReps()
        {
            var created = await _service.CreateAsync(_workoutExerciseId, 100, 10);

            var updated = await _service.UpdateAsync(created.Id, weight: 120);

            Assert.That(updated.Weight, Is.EqualTo(120));
            Assert.That(updated.Reps, Is.EqualTo(10));
        }

        [Test]
        public async Task UpdateAsync_WithOnlyRepsProvided_KeepsExistingWeight()
        {
            var created = await _service.CreateAsync(_workoutExerciseId, 100, 10);

            var updated = await _service.UpdateAsync(created.Id, reps: 12);

            Assert.That(updated.Weight, Is.EqualTo(100));
            Assert.That(updated.Reps, Is.EqualTo(12));
        }

        [Test]
        public async Task UpdateAsync_WithNoValuesProvided_LeavesSetUnchanged()
        {
            var created = await _service.CreateAsync(_workoutExerciseId, 100, 10);

            var updated = await _service.UpdateAsync(created.Id);

            Assert.That(updated.Weight, Is.EqualTo(100));
            Assert.That(updated.Reps, Is.EqualTo(10));
        }

        [Test]
        public async Task UpdateAsync_WithNegativeWeight_ThrowsArgumentExceptionAndDoesNotPersist()
        {
            var created = await _service.CreateAsync(_workoutExerciseId, 100, 10);

            Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateAsync(created.Id, weight: -5));

            var stored = await _context.Sets.FindAsync(created.Id);
            Assert.That(stored.Weight, Is.EqualTo(100));
        }

        [Test]
        public async Task UpdateAsync_WithNegativeReps_ThrowsArgumentExceptionAndDoesNotPersist()
        {
            var created = await _service.CreateAsync(_workoutExerciseId, 100, 10);

            Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateAsync(created.Id, reps: -5));

            var stored = await _context.Sets.FindAsync(created.Id);
            Assert.That(stored.Reps, Is.EqualTo(10));
        }

        [Test]
        public void UpdateAsync_WithNonExistentId_ThrowsKeyNotFoundException()
        {
            Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.UpdateAsync(id: 9999, weight: 50));
        }

        [Test]
        public async Task DeleteAsync_WithExistingId_RemovesSet()
        {
            var created = await _service.CreateAsync(_workoutExerciseId, 100, 10);

            await _service.DeleteAsync(created.Id);

            var stored = await _context.Sets.FindAsync(created.Id);
            Assert.That(stored, Is.Null);
        }

        [Test]
        public void DeleteAsync_WithNonExistentId_ThrowsKeyNotFoundException()
        {
            Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.DeleteAsync(9999));
        }
    }
}