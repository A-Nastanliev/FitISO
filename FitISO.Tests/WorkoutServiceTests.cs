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

            _service = new WorkoutService(_contextFactory);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [Test]
        public async Task CreateAsync_PersistsWorkoutWithNullStartAndEndTime()
        {
            var result = await _service.CreateAsync("Push Template", workoutExercises: null);

            Assert.That(result.Id, Is.GreaterThan(0));
            Assert.That(result.Name, Is.EqualTo("Push Template"));
            Assert.That(result.StartTime, Is.Null);
            Assert.That(result.EndTime, Is.Null);

            var stored = await _context.Workouts.FindAsync(result.Id);
            Assert.That(stored, Is.Not.Null);
        }

        [Test]
        public async Task CreateAsync_WithNullWorkoutExercises_ReturnsEmptyExerciseList()
        {
            var result = await _service.CreateAsync("Push Template", workoutExercises: null);

            Assert.That(result.WorkoutExercises, Is.Empty);
        }

        [Test]
        public async Task CreateAsync_WithWorkoutExercises_PersistsExercisesAndSets()
        {
            var exercise = new Exercise { Name = "Bench Press" };
            _context.Exercises.Add(exercise);
            await _context.SaveChangesAsync();

            var result = await _service.CreateAsync("Push Template", new List<WorkoutExercise>
            {
                new WorkoutExercise { ExerciseId = exercise.Id, Sets = new List<Set> { new Set { Weight = 100, Reps = 5 } } }
            });

            Assert.That(result.WorkoutExercises.Count, Is.EqualTo(1));
            Assert.That(result.WorkoutExercises.Single().Sets.Single().Weight, Is.EqualTo(100));
        }

        [Test]
        public async Task CreateAsync_DoesNotConflictWithExistingActiveWorkout()
        {
            var template = await _service.CreateAsync("Push Template", workoutExercises: null);
            await _service.StartFromTemplateAsync(template.Id);

            var another = await _service.CreateAsync("Pull Template", workoutExercises: null);

            Assert.That(another.Id, Is.GreaterThan(0));
        }

        [Test]
        public async Task StartFromTemplateAsync_WithValidTemplate_SetsStartTimeAndCopiesExercisesAndSets()
        {
            var exercise = new Exercise { Name = "Bench Press" };
            _context.Exercises.Add(exercise);
            await _context.SaveChangesAsync();

            var template = await _service.CreateAsync("Push Template", new List<WorkoutExercise>
            {
                new WorkoutExercise
                {
                    ExerciseId = exercise.Id,
                    Note = "Warm up first",
                    Sets = new List<Set> { new Set { Weight = 100, Reps = 5 } }
                }
            });

            var started = await _service.StartFromTemplateAsync(template.Id);

            Assert.That(started.Id, Is.Not.EqualTo(template.Id));
            Assert.That(started.Name, Is.EqualTo(template.Name));
            Assert.That(started.StartTime, Is.Not.Null);
            Assert.That(started.EndTime, Is.Null);

            var copiedExercise = started.WorkoutExercises.Single();
            Assert.That(copiedExercise.ExerciseId, Is.EqualTo(exercise.Id));
            Assert.That(copiedExercise.Note, Is.EqualTo("Warm up first"));

            var copiedSet = copiedExercise.Sets.Single();
            Assert.That(copiedSet.Weight, Is.EqualTo(100));
            Assert.That(copiedSet.Reps, Is.EqualTo(5));
        }

        [Test]
        public async Task StartFromTemplateAsync_CopiesNewWorkoutExercisesAndSetsWithFreshIds()
        {
            var exercise = new Exercise { Name = "Bench Press" };
            _context.Exercises.Add(exercise);
            await _context.SaveChangesAsync();

            var template = await _service.CreateAsync("Push Template", new List<WorkoutExercise>
            {
                new WorkoutExercise { ExerciseId = exercise.Id, Sets = new List<Set> { new Set { Weight = 100, Reps = 5 } } }
            });

            var started = await _service.StartFromTemplateAsync(template.Id);

            var templateWorkoutExerciseId = template.WorkoutExercises.Single().Id;
            var templateSetId = template.WorkoutExercises.Single().Sets.Single().Id;

            var startedWorkoutExercise = started.WorkoutExercises.Single();
            Assert.That(startedWorkoutExercise.Id, Is.Not.EqualTo(templateWorkoutExerciseId));
            Assert.That(startedWorkoutExercise.Sets.Single().Id, Is.Not.EqualTo(templateSetId));

            var storedTemplate = await _context.Workouts
                .Include(w => w.WorkoutExercises).ThenInclude(we => we.Sets)
                .FirstAsync(w => w.Id == template.Id);
            Assert.That(storedTemplate.WorkoutExercises.Single().Id, Is.EqualTo(templateWorkoutExerciseId));
        }

        [Test]
        public void StartFromTemplateAsync_WithNonExistentTemplateId_ThrowsKeyNotFoundException()
        {
            Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.StartFromTemplateAsync(9999));
        }

        [Test]
        public async Task StartFromTemplateAsync_WhenTargetIsAlreadyAStartedWorkout_ThrowsInvalidOperationException()
        {
            var template = await _service.CreateAsync("Push Template", workoutExercises: null);
            var started = await _service.StartFromTemplateAsync(template.Id);
            await _service.EndWorkoutAsync(started.Id);

            Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.StartFromTemplateAsync(started.Id));
        }

        [Test]
        public async Task StartFromTemplateAsync_WithActiveWorkoutAlreadyInProgress_ThrowsInvalidOperationException()
        {
            var firstTemplate = await _service.CreateAsync("Push Template", workoutExercises: null);
            var secondTemplate = await _service.CreateAsync("Pull Template", workoutExercises: null);
            await _service.StartFromTemplateAsync(firstTemplate.Id);

            var ex = Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.StartFromTemplateAsync(secondTemplate.Id));
            Assert.That(ex.Message, Does.Contain("Push Template"));
        }

        [Test]
        public async Task StartFromTemplateAsync_AfterActiveWorkoutEnded_IsAllowed()
        {
            var firstTemplate = await _service.CreateAsync("Push Template", workoutExercises: null);
            var secondTemplate = await _service.CreateAsync("Pull Template", workoutExercises: null);

            var first = await _service.StartFromTemplateAsync(firstTemplate.Id);
            await _service.EndWorkoutAsync(first.Id);

            var second = await _service.StartFromTemplateAsync(secondTemplate.Id);

            Assert.That(second.Id, Is.GreaterThan(0));
            Assert.That(second.StartTime, Is.Not.Null);
        }

        [Test]
        public async Task GetActiveWorkoutAsync_WithActiveWorkout_ReturnsIt()
        {
            var template = await _service.CreateAsync("Push Template", workoutExercises: null);
            var started = await _service.StartFromTemplateAsync(template.Id);

            var result = await _service.GetActiveWorkoutAsync();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(started.Id));
        }

        [Test]
        public async Task GetActiveWorkoutAsync_WithNoActiveWorkout_ReturnsNull()
        {
            await _service.CreateAsync("Push Template", workoutExercises: null);

            var result = await _service.GetActiveWorkoutAsync();

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetActiveWorkoutAsync_WithEndedWorkout_ReturnsNull()
        {
            var template = await _service.CreateAsync("Push Template", workoutExercises: null);
            var started = await _service.StartFromTemplateAsync(template.Id);
            await _service.EndWorkoutAsync(started.Id);

            var result = await _service.GetActiveWorkoutAsync();

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetWorkoutsAsync_ReturnsOnlyStartedWorkoutsOrderedById()
        {
            var t1 = await _service.CreateAsync("Leg Day Template", workoutExercises: null);
            var w1 = await _service.StartFromTemplateAsync(t1.Id);
            await _service.EndWorkoutAsync(w1.Id);

            var t2 = await _service.CreateAsync("Push Day Template", workoutExercises: null);
            var w2 = await _service.StartFromTemplateAsync(t2.Id);
            await _service.EndWorkoutAsync(w2.Id);

            await _service.CreateAsync("Unused Template", workoutExercises: null);

            var result = await _service.GetWorkoutsAsync(pageSize: 10);

            Assert.That(result.Select(w => w.Id), Is.EqualTo(new[] { w1.Id, w2.Id }));
        }

        [Test]
        public async Task GetWorkoutsAsync_RespectsPageSize()
        {
            var t1 = await _service.CreateAsync("Leg Day Template", workoutExercises: null);
            var w1 = await _service.StartFromTemplateAsync(t1.Id);
            await _service.EndWorkoutAsync(w1.Id);

            var t2 = await _service.CreateAsync("Push Day Template", workoutExercises: null);
            var w2 = await _service.StartFromTemplateAsync(t2.Id);
            await _service.EndWorkoutAsync(w2.Id);

            var result = await _service.GetWorkoutsAsync(pageSize: 1);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.Single().Id, Is.EqualTo(w1.Id));
        }

        [Test]
        public async Task GetWorkoutsAsync_WithCursor_ReturnsItemsAfterCursor()
        {
            var t1 = await _service.CreateAsync("Leg Day Template", workoutExercises: null);
            var w1 = await _service.StartFromTemplateAsync(t1.Id);
            await _service.EndWorkoutAsync(w1.Id);

            var t2 = await _service.CreateAsync("Push Day Template", workoutExercises: null);
            var w2 = await _service.StartFromTemplateAsync(t2.Id);
            await _service.EndWorkoutAsync(w2.Id);

            var result = await _service.GetWorkoutsAsync(pageSize: 10, cursor: w1.Id);

            Assert.That(result.Select(w => w.Id), Is.EqualTo(new[] { w2.Id }));
        }

        [Test]
        public async Task GetWorkoutsAsync_WithNoMatchingWorkouts_ReturnsEmptyList()
        {
            await _service.CreateAsync("Template", workoutExercises: null);

            var result = await _service.GetWorkoutsAsync(pageSize: 10);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetTemplatesAsync_ReturnsOnlyTemplatesOrderedById()
        {
            var t1 = await _service.CreateAsync("Push Template", workoutExercises: null);
            var t2 = await _service.CreateAsync("Pull Template", workoutExercises: null);
            var t3 = await _service.CreateAsync("Leg Day Template", workoutExercises: null);

            var started = await _service.StartFromTemplateAsync(t3.Id);

            var result = await _service.GetTemplatesAsync(pageSize: 10);

            Assert.That(result.Select(t => t.Id), Is.EqualTo(new[] { t1.Id, t2.Id, t3.Id }));
            Assert.That(result.Select(t => t.Id), Does.Not.Contain(started.Id));
        }

        [Test]
        public async Task GetTemplatesAsync_RespectsPageSize()
        {
            await _service.CreateAsync("Push Template", workoutExercises: null);
            await _service.CreateAsync("Pull Template", workoutExercises: null);

            var result = await _service.GetTemplatesAsync(pageSize: 1);

            Assert.That(result.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetTemplatesAsync_WithNoMatchingTemplates_ReturnsEmptyList()
        {
            var t = await _service.CreateAsync("Leg Day Template", workoutExercises: null);
            var w = await _service.StartFromTemplateAsync(t.Id);
            await _service.EndWorkoutAsync(w.Id);
            await _service.DeleteAsync(t.Id);

            var result = await _service.GetTemplatesAsync(pageSize: 10);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task UpdateNameAsync_WithValidName_UpdatesAndReturnsWorkout()
        {
            var created = await _service.CreateAsync("Leg Day", workoutExercises: null);

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
        public async Task UpdateAsync_UpdatesName()
        {
            var exercise = new Exercise { Name = "Bench Press" };
            _context.Exercises.Add(exercise);
            await _context.SaveChangesAsync();

            var created = await _service.CreateAsync("Push Template", new List<WorkoutExercise>
            {
                new WorkoutExercise { ExerciseId = exercise.Id, Sets = new List<Set> { new Set { Weight = 100, Reps = 5 } } }
            });

            var incoming = created.WorkoutExercises.Select(we => new WorkoutExercise
            {
                Id = we.Id,
                ExerciseId = we.ExerciseId,
                Note = we.Note,
                Sets = we.Sets.Select(s => new Set { Id = s.Id, Weight = s.Weight, Reps = s.Reps }).ToList()
            }).ToList();

            var result = await _service.UpdateAsync(created.Id, "Push Template v2", incoming);

            Assert.That(result.Name, Is.EqualTo("Push Template v2"));
        }

        [Test]
        public void UpdateAsync_WithNonExistentId_ThrowsKeyNotFoundException()
        {
            Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.UpdateAsync(9999, "Name", new List<WorkoutExercise>()));
        }

        [Test]
        public async Task UpdateAsync_AddsNewWorkoutExercise()
        {
            var exercise = new Exercise { Name = "Bench Press" };
            var secondExercise = new Exercise { Name = "Squat" };
            _context.Exercises.AddRange(exercise, secondExercise);
            await _context.SaveChangesAsync();

            var created = await _service.CreateAsync("Push Template", new List<WorkoutExercise>
            {
                new WorkoutExercise { ExerciseId = exercise.Id, Sets = new List<Set> { new Set { Weight = 100, Reps = 5 } } }
            });

            var incoming = created.WorkoutExercises.Select(we => new WorkoutExercise
            {
                Id = we.Id,
                ExerciseId = we.ExerciseId,
                Note = we.Note,
                Sets = we.Sets.Select(s => new Set { Id = s.Id, Weight = s.Weight, Reps = s.Reps }).ToList()
            }).ToList();

            incoming.Add(new WorkoutExercise
            {
                ExerciseId = secondExercise.Id,
                Sets = new List<Set> { new Set { Weight = 60, Reps = 8 } }
            });

            var result = await _service.UpdateAsync(created.Id, created.Name, incoming);

            Assert.That(result.WorkoutExercises.Count, Is.EqualTo(2));
            Assert.That(result.WorkoutExercises.Any(we => we.ExerciseId == secondExercise.Id), Is.True);
        }

        [Test]
        public async Task UpdateAsync_RemovesWorkoutExerciseNotInIncomingList()
        {
            var exercise = new Exercise { Name = "Bench Press" };
            _context.Exercises.Add(exercise);
            await _context.SaveChangesAsync();

            var created = await _service.CreateAsync("Push Template", new List<WorkoutExercise>
            {
                new WorkoutExercise { ExerciseId = exercise.Id, Sets = new List<Set> { new Set { Weight = 100, Reps = 5 } } }
            });

            var result = await _service.UpdateAsync(created.Id, created.Name, new List<WorkoutExercise>());

            Assert.That(result.WorkoutExercises, Is.Empty);
        }

        [Test]
        public async Task UpdateAsync_RemovedWorkoutExercise_IsDeletedFromDatabase()
        {
            var exercise = new Exercise { Name = "Bench Press" };
            _context.Exercises.Add(exercise);
            await _context.SaveChangesAsync();

            var created = await _service.CreateAsync("Push Template", new List<WorkoutExercise>
            {
                new WorkoutExercise { ExerciseId = exercise.Id, Sets = new List<Set> { new Set { Weight = 100, Reps = 5 } } }
            });
            var removedId = created.WorkoutExercises.Single().Id;

            await _service.UpdateAsync(created.Id, created.Name, new List<WorkoutExercise>());

            var stillThere = await _context.WorkoutExercises.FindAsync(removedId);
            Assert.That(stillThere, Is.Null);
        }

        [Test]
        public async Task UpdateAsync_UpdatesExistingSetValues()
        {
            var exercise = new Exercise { Name = "Bench Press" };
            _context.Exercises.Add(exercise);
            await _context.SaveChangesAsync();

            var created = await _service.CreateAsync("Push Template", new List<WorkoutExercise>
            {
                new WorkoutExercise { ExerciseId = exercise.Id, Sets = new List<Set> { new Set { Weight = 100, Reps = 5 } } }
            });

            var incoming = created.WorkoutExercises.Select(we => new WorkoutExercise
            {
                Id = we.Id,
                ExerciseId = we.ExerciseId,
                Note = we.Note,
                Sets = we.Sets.Select(s => new Set { Id = s.Id, Weight = s.Weight, Reps = s.Reps }).ToList()
            }).ToList();
            incoming.Single().Sets.Single().Weight = 120;
            incoming.Single().Sets.Single().Reps = 3;

            var result = await _service.UpdateAsync(created.Id, created.Name, incoming);

            var set = result.WorkoutExercises.Single().Sets.Single();
            Assert.That(set.Weight, Is.EqualTo(120));
            Assert.That(set.Reps, Is.EqualTo(3));
        }

        [Test]
        public async Task UpdateAsync_AddsNewSetToExistingExercise()
        {
            var exercise = new Exercise { Name = "Bench Press" };
            _context.Exercises.Add(exercise);
            await _context.SaveChangesAsync();

            var created = await _service.CreateAsync("Push Template", new List<WorkoutExercise>
            {
                new WorkoutExercise { ExerciseId = exercise.Id, Sets = new List<Set> { new Set { Weight = 100, Reps = 5 } } }
            });

            var incoming = created.WorkoutExercises.Select(we => new WorkoutExercise
            {
                Id = we.Id,
                ExerciseId = we.ExerciseId,
                Note = we.Note,
                Sets = we.Sets.Select(s => new Set { Id = s.Id, Weight = s.Weight, Reps = s.Reps }).ToList()
            }).ToList();
            incoming.Single().Sets.Add(new Set { Weight = 110, Reps = 4 });

            var result = await _service.UpdateAsync(created.Id, created.Name, incoming);

            Assert.That(result.WorkoutExercises.Single().Sets.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task UpdateAsync_RemovesSetNotInIncomingList()
        {
            var exercise = new Exercise { Name = "Bench Press" };
            _context.Exercises.Add(exercise);
            await _context.SaveChangesAsync();

            var created = await _service.CreateAsync("Push Template", new List<WorkoutExercise>
            {
                new WorkoutExercise
                {
                    ExerciseId = exercise.Id,
                    Sets = new List<Set> { new Set { Weight = 100, Reps = 5 }, new Set { Weight = 110, Reps = 3 } }
                }
            });

            var keptSet = created.WorkoutExercises.Single().Sets.First();

            var incoming = new List<WorkoutExercise>
            {
                new WorkoutExercise
                {
                    Id = created.WorkoutExercises.Single().Id,
                    ExerciseId = exercise.Id,
                    Sets = new List<Set> { new Set { Id = keptSet.Id, Weight = keptSet.Weight, Reps = keptSet.Reps } }
                }
            };

            var result = await _service.UpdateAsync(created.Id, created.Name, incoming);

            Assert.That(result.WorkoutExercises.Single().Sets.Count, Is.EqualTo(1));
            Assert.That(result.WorkoutExercises.Single().Sets.Single().Id, Is.EqualTo(keptSet.Id));
        }

        [Test]
        public async Task UpdateAsync_RemovedSet_IsDeletedFromDatabase()
        {
            var exercise = new Exercise { Name = "Bench Press" };
            _context.Exercises.Add(exercise);
            await _context.SaveChangesAsync();

            var created = await _service.CreateAsync("Push Template", new List<WorkoutExercise>
            {
                new WorkoutExercise { ExerciseId = exercise.Id, Sets = new List<Set> { new Set { Weight = 100, Reps = 5 } } }
            });
            var removedSetId = created.WorkoutExercises.Single().Sets.Single().Id;

            var incoming = new List<WorkoutExercise>
            {
                new WorkoutExercise { Id = created.WorkoutExercises.Single().Id, ExerciseId = exercise.Id, Sets = new List<Set>() }
            };

            await _service.UpdateAsync(created.Id, created.Name, incoming);

            var stillThere = await _context.Sets.FindAsync(removedSetId);
            Assert.That(stillThere, Is.Null);
        }

        [Test]
        public async Task UpdateAsync_ChangesExerciseIdAndNoteOnExistingWorkoutExercise()
        {
            var exercise = new Exercise { Name = "Bench Press" };
            var newExercise = new Exercise { Name = "Deadlift" };
            _context.Exercises.AddRange(exercise, newExercise);
            await _context.SaveChangesAsync();

            var created = await _service.CreateAsync("Push Template", new List<WorkoutExercise>
            {
                new WorkoutExercise { ExerciseId = exercise.Id, Sets = new List<Set> { new Set { Weight = 100, Reps = 5 } } }
            });

            var incoming = created.WorkoutExercises.Select(we => new WorkoutExercise
            {
                Id = we.Id,
                ExerciseId = newExercise.Id,
                Note = "Go heavy",
                Sets = we.Sets.Select(s => new Set { Id = s.Id, Weight = s.Weight, Reps = s.Reps }).ToList()
            }).ToList();

            var result = await _service.UpdateAsync(created.Id, created.Name, incoming);

            var we2 = result.WorkoutExercises.Single();
            Assert.That(we2.ExerciseId, Is.EqualTo(newExercise.Id));
            Assert.That(we2.Note, Is.EqualTo("Go heavy"));
        }


        [Test]
        public async Task EndWorkoutAsync_WithActiveWorkout_SetsEndTime()
        {
            var template = await _service.CreateAsync("Leg Day Template", workoutExercises: null);
            var started = await _service.StartFromTemplateAsync(template.Id);

            var ended = await _service.EndWorkoutAsync(started.Id);

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
            var template = await _service.CreateAsync("Template", workoutExercises: null);

            Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.EndWorkoutAsync(template.Id));
        }

        [Test]
        public async Task EndWorkoutAsync_WithAlreadyEndedWorkout_ThrowsInvalidOperationException()
        {
            var template = await _service.CreateAsync("Leg Day Template", workoutExercises: null);
            var started = await _service.StartFromTemplateAsync(template.Id);
            await _service.EndWorkoutAsync(started.Id);

            Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.EndWorkoutAsync(started.Id));
        }


        [Test]
        public async Task DeleteAsync_WithExistingId_RemovesWorkout()
        {
            var created = await _service.CreateAsync("Leg Day", workoutExercises: null);

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