using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Views;
using Android.Widget;

namespace FitISO.Maui.Platforms.Android
{
    [BroadcastReceiver(Label = "Days Since Last Workout", Exported = false)]
    [IntentFilter(new[] { AppWidgetManager.ActionAppwidgetUpdate, WidgetTheme.ActionThemeChanged })]
    [MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/last_workout_date_widget_provider")]
    public class LastWorkoutWidgetProvider : AppWidgetProvider
    {
        public const string ActionRefresh = "com.fitiso.maui.widget.DAYS_SINCE_LAST_WORKOUT_REFRESH";
        public const string LastWorkoutDateKey = "last_workout_date_ticks_utc";

        public override void OnReceive(Context? context, Intent? intent)
        {
            base.OnReceive(context, intent);

            if (context is null || (intent?.Action != ActionRefresh && intent?.Action != WidgetTheme.ActionThemeChanged))
                return;

            var manager = AppWidgetManager.GetInstance(context);
            var ids = manager.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(LastWorkoutWidgetProvider))));
            OnUpdate(context, manager, ids);
        }

        public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
        {
            if (context is null || appWidgetManager is null || appWidgetIds is null || appWidgetIds.Length == 0)
                return;

            var lastWorkoutUtc = ReadLastWorkoutDate(context);

            foreach (var widgetId in appWidgetIds)
            {
                var views = new RemoteViews(context.PackageName, Resource.Layout.last_workout_date_widget_layout);
                ApplyToViews(context, views, lastWorkoutUtc);
                appWidgetManager.UpdateAppWidget(widgetId, views);
            }
        }

        static DateTime? ReadLastWorkoutDate(Context context)
        {
            var prefs = context.GetSharedPreferences(FavouriteExerciseHistoryWidgetProvider.PrefsName, FileCreationMode.Private);
            if (prefs is null || !prefs.Contains(LastWorkoutDateKey))
                return null;

            var ticks = prefs.GetLong(LastWorkoutDateKey, 0);
            return ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Utc);
        }

        static void ApplyToViews(Context context, RemoteViews views, DateTime? lastWorkoutUtc)
        {
            var themePrefs = WidgetTheme.Prefs(context);
            var backgroundArgb = WidgetTheme.BackgroundColor(context, themePrefs);
            var accentArgb = WidgetTheme.AccentColor(context, themePrefs);

            views.ApplyTint(Resource.Id.widget_root, backgroundArgb);
            views.SetTextColor(Resource.Id.widget_days_since, new global::Android.Graphics.Color(accentArgb));

            if (lastWorkoutUtc is null)
            {
                views.SetTextViewText(Resource.Id.widget_title, "Last Workout");
                views.SetTextViewText(Resource.Id.widget_days_since, "—");
                return;
            }

            var lastLocalDate = lastWorkoutUtc.Value.ToLocalTime().Date;
            var todayLocalDate = DateTime.Now.Date;
            var days = Math.Max(0, (todayLocalDate - lastLocalDate).Days);

            var (title, value) = days switch
            {
                0 => ("Last Workout", "Today"),
                1 => ("Last Workout", "Yesterday"),
                _ => ("Days Since Last Workout", days.ToString())
            };

            views.SetTextViewText(Resource.Id.widget_title, title);
            views.SetTextViewText(Resource.Id.widget_days_since, value);
        }
    }
}