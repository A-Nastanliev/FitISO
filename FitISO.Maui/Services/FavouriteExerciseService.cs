using FitISO.Maui.Messages;
using FitISO.Maui.Models;
using CommunityToolkit.Mvvm.Messaging;
#if ANDROID
using Android.Content;
using FitISO.Maui.Platforms.Android;
using System.Linq;
using System.Text.Json;
#endif

namespace FitISO.Maui.Services
{
    public class FavoriteExerciseService : IRecipient<DbImportedMessage>
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

#if ANDROID
        static void WriteWidgetSnapshot(Exercise? exercise)
        {
            var prefs = global::Android.App.Application.Context.GetSharedPreferences(
                FavouriteExerciseWidgetProvider.PrefsName, FileCreationMode.Private);
            using var editor = prefs!.Edit();

            if (exercise is null)
            {
                editor!.Remove(FavouriteExerciseWidgetProvider.SnapshotKey);
            }
            else
            {
                var snapshot = exercise;
                editor!.PutString(FavouriteExerciseWidgetProvider.SnapshotKey, JsonSerializer.Serialize(snapshot));
            }

            editor!.Apply();
        }

        static void RefreshWidget()
        {
            var context = global::Android.App.Application.Context;
            var intent = new Intent(context, typeof(FavouriteExerciseWidgetProvider));
            intent.SetAction(FavouriteExerciseWidgetProvider.ActionRefresh);
            context.SendBroadcast(intent);
        }
#else
        static void WriteWidgetSnapshot(Exercise? exercise) { }
        static void RefreshWidget() { }
#endif
    }
}
