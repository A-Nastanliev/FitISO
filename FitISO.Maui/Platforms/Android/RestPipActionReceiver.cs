using Android.Content;

namespace FitISO.Maui.Platforms.Android
{
    class RestPipActionReceiver : BroadcastReceiver
    {
        readonly Action onToggle;
        readonly Action onReset;

        public RestPipActionReceiver(Action onToggle, Action onReset)
        {
            this.onToggle = onToggle;
            this.onReset = onReset;
        }

        public override void OnReceive(Context? context, Intent? intent)
        {
            switch (intent?.Action)
            {
                case MainActivity.ActionPipToggleRest:
                    onToggle();
                    break;
                case MainActivity.ActionPipResetRest:
                    onReset();
                    break;
            }
        }
    }
}