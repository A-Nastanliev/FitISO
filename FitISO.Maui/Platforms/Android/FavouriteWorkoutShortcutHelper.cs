using Android.Content;
using AndroidX.Core.Content.PM;
using AndroidX.Core.Graphics.Drawable;
using FitISO.Maui.Models;

namespace FitISO.Maui.Platforms.Android
{
    public static class FavouriteWorkoutShortcutHelper
    {
        const string ShortcutId = "favourite_workout";

        public static void Update(Workout? template)
        {
            var context = global::Android.App.Application.Context;

            if (template is null || string.IsNullOrEmpty(template.Name))
            {
                ShortcutManagerCompat.RemoveDynamicShortcuts(context, new List<string> { ShortcutId });
                return;
            }

            var launchIntent = new Intent(context, typeof(MainActivity));
            launchIntent.SetAction(FavouriteWorkoutStartWidgetProvider.ActionStartWorkout);
            launchIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);

            var icon = IconCompat.CreateWithResource(context, Resource.Drawable.ic_play_arrow);

            var shortcut = new ShortcutInfoCompat.Builder(context, ShortcutId)
                .SetShortLabel(template.Name)            
                .SetLongLabel($"Start {template.Name}")       
                .SetIcon(icon)
                .SetIntent(launchIntent)
                .Build();

            ShortcutManagerCompat.PushDynamicShortcut(context, shortcut);
        }
    }
}
    