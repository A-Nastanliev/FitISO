using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Maui.Models;
using FitISO.Services;
#if ANDROID
using Android.Content;
using FitISO.Maui.Platforms.Android;
#endif

namespace FitISO.Maui.Services
{
    public class FavouriteWorkoutTemplateService : IRecipient<DbImportedMessage>, IRecipient<WorkoutTemplateUpdatedMessage>
    {
        const string FavoriteTemplateIdKey = "FavouriteWorkoutTemplateId";

        readonly WorkoutService workoutService;

        public FavouriteWorkoutTemplateService(WorkoutService workoutService)
        {
            this.workoutService = workoutService;
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        public async Task<int?> GetFavoriteTemplateIdAsync()
        {
            var stored = await SecureStorage.Default.GetAsync(FavoriteTemplateIdKey);
            return int.TryParse(stored, out var id) ? id : null;
        }

        public async Task<bool> IsFavoriteAsync(int templateId)
        {
            var favoriteId = await GetFavoriteTemplateIdAsync();
            return favoriteId == templateId;
        }

        public async Task SetFavoriteAsync(Workout template)
        {
            await SecureStorage.Default.SetAsync(FavoriteTemplateIdKey, template.Id.ToString());
            WriteWidgetSnapshot(template.Name);
            RefreshWidget();
        }

        public void ClearFavorite()
        {
            SecureStorage.Default.Remove(FavoriteTemplateIdKey);
            WriteWidgetSnapshot(null);
            RefreshWidget();
        }

        public async Task ClearIfFavoriteAsync(int templateId)
        {
            if (await IsFavoriteAsync(templateId))
                ClearFavorite();
        }

        public async Task TryStartFavouriteWorkoutAsync()
        {
            if (ActiveWorkoutState.Instance.HasActiveWorkout)
                return;

            var templateId = await GetFavoriteTemplateIdAsync();
            if (templateId is null)
                return;

            try
            {
                var started = new Workout(await workoutService.StartFromTemplateAsync(templateId.Value));

                if (ActiveWorkoutState.Instance.HasActiveWorkout)
                {
                    await workoutService.DeleteAsync(started.Id);
                    return;
                }

                WeakReferenceMessenger.Default.Send(new WorkoutStartedMessage(started));
                ActiveWorkoutState.Instance.HasActiveWorkout = true;

                await WaitForShellAsync();
            }
            catch (KeyNotFoundException)
            {
                ClearFavorite();
            }
            catch (InvalidOperationException)
            {
            }
        }

        static async Task WaitForShellAsync()
        {
            var attempts = 0;
            while (Shell.Current is null && attempts++ < 50)
                await Task.Delay(100);
        }

        public void Receive(DbImportedMessage message)
        {
            ClearFavorite();
        }

        public async void Receive(WorkoutTemplateUpdatedMessage message)
        {
            if (await IsFavoriteAsync(message.Value.Id))
            {
                WriteWidgetSnapshot(message.Value.Name);
                RefreshWidget();
            }
        }

#if ANDROID
        static void WriteWidgetSnapshot(string? templateName)
        {
            var prefs = global::Android.App.Application.Context.GetSharedPreferences(
                FavouriteWorkoutStartWidgetProvider.PrefsName, FileCreationMode.Private);
            using var editor = prefs!.Edit();

            if (string.IsNullOrEmpty(templateName))
                editor!.Remove(FavouriteWorkoutStartWidgetProvider.TemplateNameKey);
            else
                editor!.PutString(FavouriteWorkoutStartWidgetProvider.TemplateNameKey, templateName);

            editor!.Apply();
        }

        static void RefreshWidget()
        {
            var context = global::Android.App.Application.Context;
            var intent = new Intent(context, typeof(FavouriteWorkoutStartWidgetProvider));
            intent.SetAction(FavouriteWorkoutStartWidgetProvider.ActionRefresh);
            context.SendBroadcast(intent);
        }
#else
        static void WriteWidgetSnapshot(string? templateName) { }
        static void RefreshWidget() { }
#endif
    }
}
