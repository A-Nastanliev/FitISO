using CommunityToolkit.Mvvm.Messaging;
using FitISO.Maui.Messages;
using FitISO.Services;
using System.Text.Json;
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
            WriteLastWorkoutSnapshot(message.Value); 
            RefreshWidget();
        }

        public async void Receive(DbImportedMessage message)
        {
            var mostRecent = await workoutService.GetWorkoutsAsync(1, null);
            var latest = mostRecent.Count > 0 ? mostRecent[0] : null;

            if (latest?.EndTime is DateTime endTime)
            {
                WriteLastWorkoutDate(endTime);
                WriteLastWorkoutSnapshot(new FitISO.Maui.Models.Workout(latest));
            }
            else
            {
                ClearLastWorkoutDate();
                ClearLastWorkoutSnapshot();
            }

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

        static void WriteLastWorkoutSnapshot(FitISO.Maui.Models.Workout workout)
        {
            var json = JsonSerializer.Serialize(workout);
            var prefs = global::Android.App.Application.Context.GetSharedPreferences(
                FavouriteExerciseHistoryWidgetProvider.PrefsName, FileCreationMode.Private);
            using var editor = prefs!.Edit();
            editor!.PutString(LastWorkoutSummaryWidgetProvider.SnapshotKey, json);
            editor!.Apply();
        }

        static void ClearLastWorkoutSnapshot()
        {
            var prefs = global::Android.App.Application.Context.GetSharedPreferences(
                FavouriteExerciseHistoryWidgetProvider.PrefsName, FileCreationMode.Private);
            using var editor = prefs!.Edit();
            editor!.Remove(LastWorkoutSummaryWidgetProvider.SnapshotKey);
            editor!.Apply();
        }

        static void RefreshWidget()
        {
            var context = global::Android.App.Application.Context;

            var intent = new Intent(context, typeof(LastWorkoutWidgetProvider));
            intent.SetAction(LastWorkoutWidgetProvider.ActionRefresh);
            context.SendBroadcast(intent);

            var summaryIntent = new Intent(context, typeof(LastWorkoutSummaryWidgetProvider));
            summaryIntent.SetAction(LastWorkoutSummaryWidgetProvider.ActionRefresh);
            context.SendBroadcast(summaryIntent);
        }
#else
        static void WriteLastWorkoutDate(DateTime utcDate) { }
        static void ClearLastWorkoutDate() { }
        static void WriteLastWorkoutSnapshot(FitISO.Maui.Models.Workout workout) { }
        static void ClearLastWorkoutSnapshot() { }
        static void RefreshWidget() { }
#endif
    }
}