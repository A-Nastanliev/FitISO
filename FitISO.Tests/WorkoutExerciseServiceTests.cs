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
    public class WorkoutExerciseServiceTests
    {
        private SqliteConnection _connection;
        private FitDbContext _context;
        private WorkoutExerciseService _service;
        private TestDbContextFactory _contextFactory;

        private int _workoutId;
        private int _exerciseId;
        private int _otherExerciseId;

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

            _service = new WorkoutExerciseService(_contextFactory);

            var workout = new Workout { Name = "Test Workout" };
            var exercise = new Exercise { Name = "Bench Press" };
            var otherExercise = new Exercise { Name = "Squat" };
            _context.AddRange(workout, exercise, otherExercise);
            await _context.SaveChangesAsync();

            _workoutId = workout.Id;
            _exerciseId = exercise.Id;
            _otherExerciseId = otherExercise.Id;
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [Test]
        public async Task CreateAsync_WithValidData_PersistsAndReturnsWorkoutExercise()
        {
            var result = await _service.CreateAsync(_workoutId, _exerciseId);

            Assert.That(result.Id, Is.GreaterThan(0));
            Assert.That(result.WorkoutId, Is.EqualTo(_workoutId));
            Assert.That(result.ExerciseId, Is.EqualTo(_exerciseId));

            var stored = await _context.WorkoutExercises.FindAsync(result.Id);
            Assert.That(stored, Is.Not.Null);
        }

        [Test]
        public void CreateAsync_WithNonExistentWorkoutId_ThrowsDbUpdateException()
        {
            Assert.ThrowsAsync<DbUpdateException>(
                () => _service.CreateAsync(workoutId: 9999, exerciseId: _exerciseId));
        }

        [Test]
        public void CreateAsync_WithNonExistentExerciseId_ThrowsDbUpdateException()
        {
            Assert.ThrowsAsync<DbUpdateException>(
                () => _service.CreateAsync(workoutId: _workoutId, exerciseId: 9999));
        }

        [Test]
        public async Task UpdateAsync_WithValidExerciseId_UpdatesAndPersists()
        {
            var created = await _service.CreateAsync(_workoutId, _exerciseId);

            var result = await _service.UpdateAsync(created.Id, exerciseId: _otherExerciseId);

            Assert.That(result.ExerciseId, Is.EqualTo(_otherExerciseId));

            var stored = await _context.WorkoutExercises.FindAsync(created.Id);
            Assert.That(stored.ExerciseId, Is.EqualTo(_otherExerciseId));
        }

        [Test]
        public async Task UpdateAsync_WithNoExerciseId_LeavesExerciseIdUnchanged()
        {
            var created = await _service.CreateAsync(_workoutId, _exerciseId);

            var result = await _service.UpdateAsync(created.Id);

            Assert.That(result.ExerciseId, Is.EqualTo(_exerciseId));
        }

        [Test]
        public void UpdateAsync_WithNonExistentId_ThrowsKeyNotFoundException()
        {
            Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.UpdateAsync(9999, exerciseId: _exerciseId));
        }

        [Test]
        public void UpdateAsync_WithNonExistentExerciseId_ThrowsDbUpdateException()
        {
            Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                var created = await _service.CreateAsync(_workoutId, _exerciseId);
                await _service.UpdateAsync(created.Id, exerciseId: 9999);
            });
        }

        [Test]
        public async Task DeleteAsync_WithExistingId_RemovesWorkoutExercise()
        {
            var created = await _service.CreateAsync(_workoutId, _exerciseId);

            await _service.DeleteAsync(created.Id);

            var stored = await _context.WorkoutExercises.FindAsync(created.Id);
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