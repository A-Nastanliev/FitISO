using Android.App;
using Android.Content;
using Android.Widget;
using FitISO.Maui.Models;
using System.Text.Json;

namespace FitISO.Maui.Platforms.Android
{
    [Service(Permission = "android.permission.BIND_REMOTEVIEWS", Exported = false)]
    public class LastWorkoutSummaryRemoteViewsService : RemoteViewsService
    {
        public override IRemoteViewsFactory OnGetViewFactory(Intent? intent) =>
            new Factory(ApplicationContext!);

        class Factory : Java.Lang.Object, IRemoteViewsFactory
        {
            const int DefaultAccentArgb = unchecked((int)0xFFCD5C5C);
            const int DefaultGridArgb = unchecked((int)0xFFDDDDDD);

            readonly Context context;
            List<Row> rows = new();
            int accentArgb = DefaultAccentArgb;
            int gridArgb = DefaultGridArgb;

            public Factory(Context context) => this.context = context;

            record Row(bool IsHeader, string Text);

            public void OnCreate() => Load();

            public void OnDataSetChanged() => Load();

            void Load()
            {
                var prefs = context.GetSharedPreferences(FavouriteExerciseHistoryWidgetProvider.PrefsName, FileCreationMode.Private);
                var json = prefs?.GetString(LastWorkoutSummaryWidgetProvider.SnapshotKey, null);

                accentArgb = prefs?.GetInt(FavouriteExerciseHistoryWidgetProvider.AccentColorKey, DefaultAccentArgb) ?? DefaultAccentArgb;
                gridArgb = prefs?.GetInt(FavouriteExerciseHistoryWidgetProvider.GridColorKey, DefaultGridArgb) ?? DefaultGridArgb;

                var newRows = new List<Row>();

                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        var workout = JsonSerializer.Deserialize<Workout>(json);
                        if (workout is not null)
                        {
                            foreach (var we in workout.WorkoutExercises)
                            {
                                newRows.Add(new Row(true, we.Exercise?.Name ?? "Exercise"));

                                var i = 1;
                                foreach (var set in we.Sets)
                                    newRows.Add(new Row(false, $"{i++}.  {FormatSet(set)}"));
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                rows = newRows;
            }

            static string FormatSet(Set set)
            {
                var hasWeight = set.Weight is > 0;
                var hasReps = set.Reps is > 0;

                if (hasWeight && hasReps)
                    return $"{set.Weight:0.##} kg × {set.Reps:0.##}";
                if (hasReps)
                    return $"{set.Reps:0.##} reps";
                if (hasWeight)
                    return $"{set.Weight:0.##} kg";
                return "—";
            }

            public int Count => rows.Count;
            public bool HasStableIds => true;
            public long GetItemId(int position) => position;
            public int ViewTypeCount => 2;
            public RemoteViews? LoadingView => null;
            public void OnDestroy() { }

            public RemoteViews GetViewAt(int position)
            {
                var row = rows[position];

                if (row.IsHeader)
                {
                    var views = new RemoteViews(context.PackageName, Resource.Layout.last_workout_summary_widget_header_item);
                    views.SetTextViewText(Resource.Id.row_exercise_name, row.Text);
                    views.SetTextColor(Resource.Id.row_exercise_name, new global::Android.Graphics.Color(accentArgb));
                    return views;
                }
                else
                {
                    var views = new RemoteViews(context.PackageName, Resource.Layout.last_workout_summary_widget_set_item);
                    views.SetTextViewText(Resource.Id.row_set_text, row.Text);
                    views.SetTextColor(Resource.Id.row_set_text, new global::Android.Graphics.Color(gridArgb));
                    return views;
                }
            }

            public int GetViewTypeOf(int position) => rows[position].IsHeader ? 0 : 1;
        }
    }
}