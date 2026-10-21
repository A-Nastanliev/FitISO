using FitISO.Maui.Models;
using FitISO.Sharing.Snapshots;

namespace FitISO.Maui.Services
{
    public static class WorkoutSnapshotMapper
    {
        public static WorkoutSnapshot Map(Workout workout, DateTime? restStartTime, bool restIsStopped, long version)
        {
            var exercises = workout.WorkoutExercises
                .Select(we => new ExerciseSnapshot(
                    we.Exercise?.Name ?? string.Empty,
                    we.Sets
                        .Select(s => new SetSnapshot(s.Weight, s.Reps, s.IsComplete))
                        .ToList()))
                .ToList();

            return new WorkoutSnapshot(
                version, workout.Name ?? string.Empty, ToUtc(workout.StartTime), ToUtc(workout.EndTime),
                exercises, new RestStopwatchSnapshot(ToUtc(restStartTime), restIsStopped));
        }

        static DateTime? ToUtc(DateTime? value) =>
            value is DateTime dt
                ? (dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc))
                : null;
    }
}