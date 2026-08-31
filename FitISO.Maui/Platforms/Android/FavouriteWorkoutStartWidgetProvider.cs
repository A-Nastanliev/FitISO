using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Views;
using Android.Widget;

namespace FitISO.Maui.Platforms.Android
{
    [BroadcastReceiver(Label = "Favourite Workout", Exported = false)]
    [IntentFilter(new[] { AppWidgetManager.ActionAppwidgetUpdate })]
    [MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/favourite_workout_start_widget_provider")]
    public class FavouriteWorkoutStartWidgetProvider : AppWidgetProvider
    {
        public const string ActionRefresh = "com.fitiso.maui.widget.FAVOURITE_WORKOUT_START_REFRESH";
        public const string ActionStartWorkout = "com.fitiso.maui.widget.START_FAVOURITE_WORKOUT";
        public const string PrefsName = "FitISO.FavouriteWorkoutWidget";
        public const string TemplateNameKey = "template_name";

        const int PlayIconArgb = unchecked((int)0xFF212121);
        const int DefaultBackgroundArgb = unchecked((int)0xFF1E1E1E);
        const int DefaultAccentArgb = unchecked((int)0xFFCD5C5C);

        public override void OnReceive(Context? context, Intent? intent)
        {
            base.OnReceive(context, intent);

            if (context is null || intent?.Action != ActionRefresh)
                return;

            var manager = AppWidgetManager.GetInstance(context);
            var ids = manager.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(FavouriteWorkoutStartWidgetProvider))));
            OnUpdate(context, manager, ids);
        }

        public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
        {
            if (context is null || appWidgetManager is null || appWidgetIds is null || appWidgetIds.Length == 0)
                return;

            var name = ReadFavoriteName(context);

            foreach (var widgetId in appWidgetIds)
            {
                var views = new RemoteViews(context.PackageName, Resource.Layout.favourite_workout_start_widget_layout);
                ApplyToViews(context, views, name, widgetId);
                appWidgetManager.UpdateAppWidget(widgetId, views);
            }
        }

        static string? ReadFavoriteName(Context context)
        {
            var prefs = context.GetSharedPreferences(PrefsName, FileCreationMode.Private);
            return prefs?.GetString(TemplateNameKey, null);
        }

        static void ApplyToViews(Context context, RemoteViews views, string? name, int widgetId)
        {
            var themePrefs = context.GetSharedPreferences(FavouriteExerciseHistoryWidgetProvider.PrefsName, FileCreationMode.Private);
            var backgroundArgb = themePrefs?.GetInt(FavouriteExerciseHistoryWidgetProvider.BackgroundColorKey, DefaultBackgroundArgb) ?? DefaultBackgroundArgb;
            var accentArgb = themePrefs?.GetInt(FavouriteExerciseHistoryWidgetProvider.AccentColorKey, DefaultAccentArgb) ?? DefaultAccentArgb;

            ApplyTint(views, Resource.Id.widget_root, backgroundArgb);

            if (string.IsNullOrEmpty(name))
            {
                views.SetViewVisibility(Resource.Id.widget_title, ViewStates.Gone);
                views.SetViewVisibility(Resource.Id.widget_start_button, ViewStates.Gone);
                views.SetViewVisibility(Resource.Id.widget_empty_state, ViewStates.Visible);
                return;
            }

            views.SetViewVisibility(Resource.Id.widget_empty_state, ViewStates.Gone);
            views.SetViewVisibility(Resource.Id.widget_title, ViewStates.Visible);
            views.SetViewVisibility(Resource.Id.widget_start_button, ViewStates.Visible);
            views.SetTextViewText(Resource.Id.widget_title, name);

            ApplyTint(views, Resource.Id.widget_start_button, accentArgb);
            views.SetInt(Resource.Id.widget_start_button, "setColorFilter", PlayIconArgb);

            var launchIntent = new Intent(context, typeof(MainActivity));
            launchIntent.SetAction(ActionStartWorkout);
            launchIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);

            var pendingIntent = PendingIntent.GetActivity(
                context,
                widgetId,
                launchIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            views.SetOnClickPendingIntent(Resource.Id.widget_start_button, pendingIntent);
        }

        static void ApplyTint(RemoteViews views, int viewId, int argb)
        {
            var androidColor = new global::Android.Graphics.Color(argb);

            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                views.SetColorStateList(viewId, "setBackgroundTintList",
                    global::Android.Content.Res.ColorStateList.ValueOf(androidColor));
            }
            else
            {
                views.SetInt(viewId, "setBackgroundColor", argb);
            }
        }
    }
}
