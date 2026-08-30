using FitISO.Maui.Messages;
using FitISO.Maui.Models;
using CommunityToolkit.Mvvm.Messaging;
using System.Linq;
#if ANDROID
using Android.Content;
using FitISO.Maui.Platforms.Android;
using System.Text.Json;
#endif

namespace FitISO.Maui.Services
{
    public class FavoriteExerciseService : IRecipient<DbImportedMessage>, IRecipient<WorkoutFinishedMessage>, IRecipient<ExerciseUpdatedMessage>
    {
        const string FavoriteExerciseIdKey = "FavoriteExerciseId";

        public FavoriteExerciseService()
        {
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        public async Task<int?> GetFavoriteExerciseIdAsync()
        {
            var stored = await SecureStorage.Default.GetAsync(FavoriteExerciseIdKey);
            return int.TryParse(stored, out var id) ? id : null;
        }

        public async Task<bool> IsFavoriteAsync(int exerciseId)
        {
            var favoriteId = await GetFavoriteExerciseIdAsync();
            return favoriteId == exerciseId;
        }

        public async Task SetFavoriteAsync(Exercise exercise)
        {
            await SecureStorage.Default.SetAsync(FavoriteExerciseIdKey, exercise.Id.ToString());
            WriteWidgetSnapshot(exercise);
            RefreshWidget();
        }

        public void ClearFavorite()
        {
            SecureStorage.Default.Remove(FavoriteExerciseIdKey);
            WriteWidgetSnapshot(null);
            RefreshWidget();
        }

        public void Receive(DbImportedMessage message)
        {
            ClearFavorite();
        }

        public async void Receive(WorkoutFinishedMessage message)
        {
            var favoriteId = await GetFavoriteExerciseIdAsync();
            if (favoriteId is null) return;

            var workoutExercise = message.Value.WorkoutExercises
                .FirstOrDefault(we => we.Exercise.Id == favoriteId.Value);

            if (workoutExercise is null || workoutExercise.Sets.Count == 0) return;

            var updated = UpdateWidgetSnapshotFromWorkout(workoutExercise, message.Value.StartTime);
            if (updated)
                RefreshWidget();
        }

        public async void Receive(ExerciseUpdatedMessage message)
        {
            var favoriteId = await GetFavoriteExerciseIdAsync();
            if (favoriteId is null || favoriteId.Value != message.Value.Id) return;

            var updated = UpdateWidgetSnapshotName(message.Value.Name);
            if (updated)
                RefreshWidget();
        }

#if ANDROID
        static void WriteWidgetSnapshot(Exercise? exercise)
        {
            var prefs = global::Android.App.Application.Context.GetSharedPreferences(
                FavouriteExerciseHistoryWidgetProvider.PrefsName, FileCreationMode.Private);
            using var editor = prefs!.Edit();

            if (exercise is null)
            {
                editor!.Remove(FavouriteExerciseHistoryWidgetProvider.SnapshotKey);
            }
            else
            {
                editor!.PutString(FavouriteExerciseHistoryWidgetProvider.SnapshotKey, JsonSerializer.Serialize(exercise));
            }

            editor!.Apply();
        }

        static Exercise? ReadWidgetSnapshot()
        {
            var prefs = global::Android.App.Application.Context.GetSharedPreferences(
                FavouriteExerciseHistoryWidgetProvider.PrefsName, FileCreationMode.Private);
            var json = prefs?.GetString(FavouriteExerciseHistoryWidgetProvider.SnapshotKey, null);
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<Exercise>(json);
            }
            catch
            {
                return null;
            }
        }

        static bool UpdateWidgetSnapshotFromWorkout(WorkoutExercise workoutExercise, DateTime? workoutStart)
        {
            var bestSetThisWorkout = workoutExercise.Sets[0];
            for (var i = 1; i < workoutExercise.Sets.Count; i++)
            {
                if (IsBetterSet(workoutExercise.Sets[i], bestSetThisWorkout))
                    bestSetThisWorkout = workoutExercise.Sets[i];
            }

            var hasUsableSet = (bestSetThisWorkout.Weight ?? 0) > 0 || (bestSetThisWorkout.Reps ?? 0) > 0;
            if (!hasUsableSet) return false;

            var snapshot = ReadWidgetSnapshot() ?? new Exercise
            {
                Id = workoutExercise.Exercise.Id,
                Name = workoutExercise.Exercise.Name
            };

            snapshot.Name = workoutExercise.Exercise.Name;

            var date = workoutStart ?? DateTime.UtcNow;
            snapshot.History.Add(new ExerciseHistoryPoint(date, bestSetThisWorkout.Weight ?? 0, bestSetThisWorkout.Reps ?? 0));

            if (snapshot.BestSet is null || IsBetterSet(bestSetThisWorkout, snapshot.BestSet))
            {
                snapshot.BestSet = new Set { Weight = bestSetThisWorkout.Weight, Reps = bestSetThisWorkout.Reps };
            }

            WriteWidgetSnapshot(snapshot);
            return true;
        }

        static bool UpdateWidgetSnapshotName(string newName)
        {
            var snapshot = ReadWidgetSnapshot();
            if (snapshot is null || snapshot.Name == newName) return false;

            snapshot.Name = newName;
            WriteWidgetSnapshot(snapshot);
            return true;
        }

        static bool IsBetterSet(Set candidate, Set currentBest)
        {
            if ((candidate.Weight ?? 0) > (currentBest.Weight ?? 0))
                return true;

            if ((candidate.Weight ?? 0) == (currentBest.Weight ?? 0) && (candidate.Reps ?? 0) > (currentBest.Reps ?? 0))
                return true;

            return false;
        }

        static void RefreshWidget()
        {
            var context = global::Android.App.Application.Context;

            var historyIntent = new Intent(context, typeof(FavouriteExerciseHistoryWidgetProvider));
            historyIntent.SetAction(FavouriteExerciseHistoryWidgetProvider.ActionRefresh);
            context.SendBroadcast(historyIntent);

            var bestSetIntent = new Intent(context, typeof(FavouriteExerciseBestSetWidgetProvider));
            bestSetIntent.SetAction(FavouriteExerciseBestSetWidgetProvider.ActionRefresh);
            context.SendBroadcast(bestSetIntent);
        }
#else
        static void WriteWidgetSnapshot(Exercise? exercise) { }
        static bool UpdateWidgetSnapshotFromWorkout(WorkoutExercise workoutExercise, DateTime? workoutStart) => false;
        static bool UpdateWidgetSnapshotName(string newName) => false;
        static void RefreshWidget() { }
#endif
    }
}