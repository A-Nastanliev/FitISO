using System;
using System.Collections.Generic;
using System.Linq;
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
    public class WorkoutServiceTests
    {
        private SqliteConnection _connection;
        private FitDbContext _context;
        private WorkoutService _service;

        [SetUp]
        public void SetUp()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<FitDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new FitDbContext(options);
            _context.Database.EnsureCreated();

            _service = new WorkoutService(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [Test]
        public async Task CreateAsync_AsWorkout_SetsStartTimeAndPersists()
        {
            var result = await _service.CreateAsync("Leg Day", isTemplate: false);

            Assert.That(result.Id, Is.GreaterThan(0));
            Assert.That(result.Name, Is.EqualTo("Leg Day"));
            Assert.That(result.StartTime, Is.Not.Null);
            Assert.That(result.EndTime, Is.Null);

            var stored = await _context.Workouts.FindAsync(result.Id);
            Assert.That(stored, Is.Not.Null);
        }

        [Test]
        public async Task CreateAsync_AsTemplate_LeavesStartTimeNull()
        {
            var result = await _service.CreateAsync("Push Template", isTemplate: true);

            Assert.That(result.StartTime, Is.Null);
            Assert.That(result.EndTime, Is.Null);
        }

        [Test]
        public async Task CreateAsync_AsTemplate_DoesNotConflictWithActiveWorkout()
        {
            await _service.CreateAsync("Leg Day", isTemplate: false);

            var template = await _service.CreateAsync("Push Template", isTemplate: true);

            Assert.That(template.Id, Is.GreaterThan(0));
        }

        [Test]
        public void CreateAsync_WithActiveWorkoutAlreadyStarted_ThrowsInvalidOperationException()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _service.CreateAsync("Leg Day", isTemplate: false);
                await _service.CreateAsync("Push Day", isTemplate: false);
            });
        }

        [Test]
        public async Task CreateAsync_AfterActiveWorkoutEnded_IsAllowed()
        {
            var first = await _service.CreateAsync("Leg Day", isTemplate: false);
            await _service.EndWorkoutAsync(first.Id);

            var second = await _service.CreateAsync("Push Day", isTemplate: false);

            Assert.That(second.Id, Is.GreaterThan(0));
        }

        [Test]
        public async Task GetActiveWorkoutAsync_WithActiveWorkout_ReturnsIt()
        {
            var created = await _service.CreateAsync("Leg Day", isTemplate: false);

            var result = await _service.GetActiveWorkoutAsync();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(created.Id));
        }

        [Test]
        public async Task GetActiveWorkoutAsync_WithNoActiveWorkout_ReturnsNull()
        {
            await _service.CreateAsync("Push Template", isTemplate: true);

            var result = await _service.GetActiveWorkoutAsync();

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetActiveWorkoutAsync_WithEndedWorkout_ReturnsNull()
        {
            var created = await _service.CreateAsync("Leg Day", isTemplate: false);
            await _service.EndWorkoutAsync(created.Id);

            var result = await _service.GetActiveWorkoutAsync();

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetWorkoutsAsync_ReturnsOnlyStartedWorkoutsOrderedById()
        {
            var w1 = await _service.CreateAsync("Leg Day", isTemplate: false);
            await _service.EndWorkoutAsync(w1.Id);
            var w2 = await _service.CreateAsync("Push Day", isTemplate: false);
            await _service.EndWorkoutAsync(w2.Id);
            await _service.CreateAsync("Template", isTemplate: true);

            var result = await _service.GetWorkoutsAsync(pageSize: 10);

            Assert.That(result.Select(w => w.Id), Is.EqualTo(new[] { w1.Id, w2.Id }));
        }

        [Test]
        public async Task GetWorkoutsAsync_RespectsPageSize()
        {
            var w1 = await _service.CreateAsync("Leg Day", isTemplate: false);
            await _service.EndWorkoutAsync(w1.Id);
            var w2 = await _service.CreateAsync("Push Day", isTemplate: false);
            await _service.EndWorkoutAsync(w2.Id);

            var result = await _service.GetWorkoutsAsync(pageSize: 1);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.Single().Id, Is.EqualTo(w1.Id));
        }

        [Test]
        public async Task GetWorkoutsAsync_WithCursor_ReturnsItemsAfterCursor()
        {
            var w1 = await _service.CreateAsync("Leg Day", isTemplate: false);
            await _service.EndWorkoutAsync(w1.Id);
            var w2 = await _service.CreateAsync("Push Day", isTemplate: false);
            await _service.EndWorkoutAsync(w2.Id);

            var result = await _service.GetWorkoutsAsync(pageSize: 10, cursor: w1.Id);

            Assert.That(result.Select(w => w.Id), Is.EqualTo(new[] { w2.Id }));
        }

        [Test]
        public async Task GetWorkoutsAsync_WithNoMatchingWorkouts_ReturnsEmptyList()
        {
            await _service.CreateAsync("Template", isTemplate: true);

            var result = await _service.GetWorkoutsAsync(pageSize: 10);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetTemplatesAsync_ReturnsOnlyTemplatesOrderedById()
        {
            var t1 = await _service.CreateAsync("Push Template", isTemplate: true);
            var t2 = await _service.CreateAsync("Pull Template", isTemplate: true);
            var w = await _service.CreateAsync("Leg Day", isTemplate: false);

            var result = await _service.GetTemplatesAsync(pageSize: 10);

            Assert.That(result.Select(t => t.Id), Is.EqualTo(new[] { t1.Id, t2.Id }));
        }

        [Test]
        public async Task GetTemplatesAsync_RespectsPageSize()
        {
            await _service.CreateAsync("Push Template", isTemplate: true);
            await _service.CreateAsync("Pull Template", isTemplate: true);

            var result = await _service.GetTemplatesAsync(pageSize: 1);

            Assert.That(result.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetTemplatesAsync_WithNoMatchingTemplates_ReturnsEmptyList()
        {
            var w = await _service.CreateAsync("Leg Day", isTemplate: false);
            await _service.EndWorkoutAsync(w.Id);

            var result = await _service.GetTemplatesAsync(pageSize: 10);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task UpdateNameAsync_WithValidName_UpdatesAndReturnsWorkout()
        {
            var created = await _service.CreateAsync("Leg Day", isTemplate: false);

            var updated = await _service.UpdateNameAsync(created.Id, "Leg Day v2");

            Assert.That(updated.Name, Is.EqualTo("Leg Day v2"));
        }

        [Test]
        public void UpdateNameAsync_WithNonExistentId_ThrowsKeyNotFoundException()
        {
            Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.UpdateNameAsync(9999, "New Name"));
        }

        [Test]
        public async Task EndWorkoutAsync_WithActiveWorkout_SetsEndTime()
        {
            var created = await _service.CreateAsync("Leg Day", isTemplate: false);

            var ended = await _service.EndWorkoutAsync(created.Id);

            Assert.That(ended.EndTime, Is.Not.Null);
        }

        [Test]
        public void EndWorkoutAsync_WithNonExistentId_ThrowsKeyNotFoundException()
        {
            Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.EndWorkoutAsync(9999));
        }

        [Test]
        public async Task EndWorkoutAsync_WithTemplate_ThrowsInvalidOperationException()
        {
            var template = await _service.CreateAsync("Template", isTemplate: true);

            Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.EndWorkoutAsync(template.Id));
        }

        [Test]
        public async Task EndWorkoutAsync_WithAlreadyEndedWorkout_ThrowsInvalidOperationException()
        {
            var created = await _service.CreateAsync("Leg Day", isTemplate: false);
            await _service.EndWorkoutAsync(created.Id);

            Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.EndWorkoutAsync(created.Id));
        }

        [Test]
        public async Task DeleteAsync_WithExistingId_RemovesWorkout()
        {
            var created = await _service.CreateAsync("Leg Day", isTemplate: false);

            await _service.DeleteAsync(created.Id);

            var stored = await _context.Workouts.FindAsync(created.Id);
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