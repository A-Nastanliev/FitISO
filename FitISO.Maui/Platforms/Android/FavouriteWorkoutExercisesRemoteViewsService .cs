using Android.App;
using Android.Content;
using Android.Widget;
using FitISO.Maui.Models;
using System.Text.Json;

namespace FitISO.Maui.Platforms.Android
{
    [Service(Permission = "android.permission.BIND_REMOTEVIEWS", Exported = false)]
    public class FavouriteWorkoutExercisesRemoteViewsService : RemoteViewsService
    {
        public override IRemoteViewsFactory OnGetViewFactory(Intent? intent) =>
            new Factory(ApplicationContext!);

        class Factory : Java.Lang.Object, IRemoteViewsFactory
        {
            const int DefaultAccentArgb = unchecked((int)0xFFCD5C5C);
            const int DefaultGridArgb = unchecked((int)0xFFDDDDDD);

            readonly Context context;
            List<WorkoutExercise> exercises = new();
            int accentArgb = DefaultAccentArgb;
            int gridArgb = DefaultGridArgb;

            public Factory(Context context) => this.context = context;

            public void OnCreate() => Load();

            public void OnDataSetChanged() => Load();

            void Load()
            {
                var prefs = context.GetSharedPreferences(FavouriteWorkoutStartWidgetProvider.PrefsName, FileCreationMode.Private);
                var json = prefs?.GetString(FavouriteWorkoutStartWidgetProvider.SnapshotKey, null);

                var themePrefs = context.GetSharedPreferences(FavouriteExerciseHistoryWidgetProvider.PrefsName, FileCreationMode.Private);
                accentArgb = themePrefs?.GetInt(FavouriteExerciseHistoryWidgetProvider.AccentColorKey, DefaultAccentArgb) ?? DefaultAccentArgb;
                gridArgb = themePrefs?.GetInt(FavouriteExerciseHistoryWidgetProvider.GridColorKey, DefaultGridArgb) ?? DefaultGridArgb;

                var newExercises = new List<WorkoutExercise>();

                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        var workout = JsonSerializer.Deserialize<Workout>(json);
                        if (workout?.WorkoutExercises is not null)
                            newExercises.AddRange(workout.WorkoutExercises);
                    }
                    catch
                    {
                    }
                }

                exercises = newExercises;
            }

            static string FormatSetCount(int setCount) => setCount == 1 ? "1 set" : $"{setCount} sets";

            public int Count => exercises.Count;
            public bool HasStableIds => true;
            public long GetItemId(int position) => position;
            public int ViewTypeCount => 1;
            public RemoteViews? LoadingView => null;
            public void OnDestroy() { }

            public RemoteViews GetViewAt(int position)
            {
                var exercise = exercises[position];
                var setCount = exercise.Sets?.Count ?? exercise.SetCount;

                var views = new RemoteViews(context.PackageName, Resource.Layout.favourite_workout_start_widget_exercise_item);
                views.SetTextViewText(Resource.Id.row_exercise_name, exercise.Exercise?.Name ?? string.Empty);
                views.SetTextColor(Resource.Id.row_exercise_name, new global::Android.Graphics.Color(accentArgb));
                views.SetTextViewText(Resource.Id.row_set_count, FormatSetCount(setCount));
                views.SetTextColor(Resource.Id.row_set_count, new global::Android.Graphics.Color(gridArgb));
                return views;
            }

            public int GetViewTypeOf(int position) => 0;
        }
    }
}
