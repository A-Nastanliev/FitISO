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

            _context = new FitDbContext(options);
            _context.Database.EnsureCreated();

            _service = new WorkoutExerciseService(_context);

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
            var result = await _service.CreateAsync(_workoutId, _exerciseId, "Felt strong today");

            Assert.That(result.Id, Is.GreaterThan(0));
            Assert.That(result.WorkoutId, Is.EqualTo(_workoutId));
            Assert.That(result.ExerciseId, Is.EqualTo(_exerciseId));
            Assert.That(result.Note, Is.EqualTo("Felt strong today"));

            var stored = await _context.WorkoutExercises.FindAsync(result.Id);
            Assert.That(stored, Is.Not.Null);
        }

        [Test]
        public async Task CreateAsync_WithNullNote_IsAllowed()
        {
            var result = await _service.CreateAsync(_workoutId, _exerciseId);

            Assert.That(result.Note, Is.Null);
        }

        [Test]
        public async Task CreateAsync_WithNoteAtMaxLength_IsAllowed()
        {
            var note = new string('a', 100);

            var result = await _service.CreateAsync(_workoutId, _exerciseId, note);

            Assert.That(result.Note, Is.EqualTo(note));
        }

        [Test]
        public void CreateAsync_WithNoteExceedingMaxLength_ThrowsArgumentException()
        {
            var note = new string('a', 101);

            var ex = Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(_workoutId, _exerciseId, note));

            Assert.That(ex.ParamName, Is.EqualTo("note"));
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
        public async Task UpdateAsync_WithNewExerciseId_UpdatesExerciseId()
        {
            var created = await _service.CreateAsync(_workoutId, _exerciseId, "Original note");

            var updated = await _service.UpdateAsync(created.Id, exerciseId: _otherExerciseId);

            Assert.That(updated.ExerciseId, Is.EqualTo(_otherExerciseId));
            Assert.That(updated.Note, Is.EqualTo("Original note"));
        }

        [Test]
        public async Task UpdateAsync_WithNewNote_UpdatesNoteOnly()
        {
            var created = await _service.CreateAsync(_workoutId, _exerciseId, "Original note");

            var updated = await _service.UpdateAsync(created.Id, note: "Updated note");

            Assert.That(updated.ExerciseId, Is.EqualTo(_exerciseId));
            Assert.That(updated.Note, Is.EqualTo("Updated note"));
        }

        [Test]
        public async Task UpdateAsync_WithEmptyStringNote_ClearsNote()
        {
            var created = await _service.CreateAsync(_workoutId, _exerciseId, "Original note");

            var updated = await _service.UpdateAsync(created.Id, note: "");

            Assert.That(updated.Note, Is.EqualTo(""));
        }

        [Test]
        public async Task UpdateAsync_WithNoArguments_LeavesWorkoutExerciseUnchanged()
        {
            var created = await _service.CreateAsync(_workoutId, _exerciseId, "Original note");

            var updated = await _service.UpdateAsync(created.Id);

            Assert.That(updated.ExerciseId, Is.EqualTo(_exerciseId));
            Assert.That(updated.Note, Is.EqualTo("Original note"));
        }

        [Test]
        public async Task UpdateAsync_WithNoteExceedingMaxLength_ThrowsArgumentExceptionAndDoesNotPersist()
        {
            var created = await _service.CreateAsync(_workoutId, _exerciseId, "Original note");
            var tooLong = new string('a', 101);

            Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateAsync(created.Id, note: tooLong));

            var stored = await _context.WorkoutExercises.FindAsync(created.Id);
            Assert.That(stored.Note, Is.EqualTo("Original note"));
        }

        [Test]
        public void UpdateAsync_WithNonExistentId_ThrowsKeyNotFoundException()
        {
            Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.UpdateAsync(id: 9999, note: "Updated note"));
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