using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Services;
#if ANDROID
using Android.Content;
using FitISO.Maui.Platforms.Android;
#endif

namespace FitISO.Maui.Services
{
    public class LastWorkoutService : IRecipient<WorkoutFinishedMessage>, IRecipient<DbImportedMessage>
    {
        readonly WorkoutService workoutService;

        public LastWorkoutService(WorkoutService workoutService)
        {
            this.workoutService = workoutService;
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        public void Receive(WorkoutFinishedMessage message)
        {
            var date = message.Value.EndTime ?? DateTime.UtcNow;
            WriteLastWorkoutDate(date);
            RefreshWidget();
        }

        public async void Receive(DbImportedMessage message)
        {
            var mostRecent = await workoutService.GetWorkoutsAsync(1, null);
            var latest = mostRecent.Count > 0 ? mostRecent[0] : null;

            if (latest?.EndTime is DateTime endTime)
                WriteLastWorkoutDate(endTime);
            else
                ClearLastWorkoutDate();

            RefreshWidget();
        }

#if ANDROID
        static void WriteLastWorkoutDate(DateTime utcDate)
        {
            var prefs = global::Android.App.Application.Context.GetSharedPreferences(
                FavouriteExerciseHistoryWidgetProvider.PrefsName, FileCreationMode.Private);
            using var editor = prefs!.Edit();
            editor!.PutLong(LastWorkoutWidgetProvider.LastWorkoutDateKey, utcDate.Ticks);
            editor!.Apply();
        }

        static void ClearLastWorkoutDate()
        {
            var prefs = global::Android.App.Application.Context.GetSharedPreferences(
                FavouriteExerciseHistoryWidgetProvider.PrefsName, FileCreationMode.Private);
            using var editor = prefs!.Edit();
            editor!.Remove(LastWorkoutWidgetProvider.LastWorkoutDateKey);
            editor!.Apply();
        }

        static void RefreshWidget()
        {
            var context = global::Android.App.Application.Context;
            var intent = new Intent(context, typeof(LastWorkoutWidgetProvider));
            intent.SetAction(LastWorkoutWidgetProvider.ActionRefresh);
            context.SendBroadcast(intent);
        }
#else
        static void WriteLastWorkoutDate(DateTime utcDate) { }
        static void ClearLastWorkoutDate() { }
        static void RefreshWidget() { }
#endif
    }
}