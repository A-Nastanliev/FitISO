using FitISO.Maui.Drawables;

namespace FitISO.Maui.Views
{
    public class SetRippleView : GraphicsView
    {
        const string AnimationName = "SetRipple";  

        readonly RippleDrawable ripple = new();
        TaskCompletionSource? pending;

        public SetRippleView()
        {
            Drawable = ripple;
            InputTransparent = true;  
        }

        public Task PlayAsync(Color color, uint duration = 450)
        {
            Cancel();

            ripple.RippleColor = color;
            var tcs = pending = new TaskCompletionSource();

            this.Animate(AnimationName,
                callback: v =>
                {
                    ripple.Progress = (float)v;
                    Invalidate();         
                },
                start: 0, end: 1, rate: 16, length: duration, easing: Easing.CubicOut,
                finished: (_, _) =>
                {
                    ripple.Progress = 0;
                    Invalidate();
                    tcs.TrySetResult();
                });

            return tcs.Task;
        }

        public void Cancel()
        {
            this.AbortAnimation(AnimationName);
            if (ripple.Progress != 0)
            {
                ripple.Progress = 0;
                Invalidate();
            }
            pending?.TrySetResult();
            pending = null;
        }
    }
}