using Ring = global::ProgressRing.Maui.ProgressRing;

namespace FitISO.Maui.Behaviors
{
    public class AnimatedProgressRingBehavior : Behavior<Ring>
    {
        const string AnimationName = "ProgressRingProgress";

        public static readonly BindableProperty TargetProgressProperty =
            BindableProperty.Create(
                nameof(TargetProgress), typeof(double), typeof(AnimatedProgressRingBehavior), 0d,
                propertyChanged: (b, _, n) => ((AnimatedProgressRingBehavior)b).Animate((double)n));

        public static readonly BindableProperty PulseTargetProperty =
            BindableProperty.Create(
                nameof(PulseTarget), typeof(VisualElement), typeof(AnimatedProgressRingBehavior));

        public double TargetProgress
        {
            get => (double)GetValue(TargetProgressProperty);
            set => SetValue(TargetProgressProperty, value);
        }

        public VisualElement? PulseTarget
        {
            get => (VisualElement?)GetValue(PulseTargetProperty);
            set => SetValue(PulseTargetProperty, value);
        }

        public uint Duration { get; set; } = 600;
        public string? DefaultColorKey { get; set; }
        public string? CompletedColorKey { get; set; }

        Ring? ring;
        bool wasComplete;

        protected override void OnAttachedTo(Ring bindable)
        {
            base.OnAttachedTo(bindable);
            ring = bindable;

            BindingContext = bindable.BindingContext;
            bindable.BindingContextChanged += OnRingBindingContextChanged;

            bindable.Progress = TargetProgress;
            ApplyColor(TargetProgress >= 1);
        }

        protected override void OnDetachingFrom(Ring bindable)
        {
            bindable.BindingContextChanged -= OnRingBindingContextChanged;
            bindable.AbortAnimation(AnimationName);
            ring = null;
            base.OnDetachingFrom(bindable);
        }

        void OnRingBindingContextChanged(object? sender, EventArgs e)
            => BindingContext = ring?.BindingContext;

        async void Animate(double value)
        {
            if (ring is null) return;
            bool complete = value >= 1;

            if (!ring.IsLoaded)
            {
                ring.AbortAnimation(AnimationName);
                ring.Progress = value;
                ApplyColor(complete);
                wasComplete = complete;
                return;
            }

            bool cancelled = await ProgressToAsync(ring, value);
            if (cancelled || ring is null) return;

            ApplyColor(complete);

            if (complete && !wasComplete)
                await PulseAsync();

            wasComplete = complete;
        }

        Task<bool> ProgressToAsync(Ring target, double value)
        {
            var completion = new TaskCompletionSource<bool>();

            target.AbortAnimation(AnimationName);

            var animation = new Animation(v => target.Progress = v, target.Progress, value, Easing.CubicOut);
            animation.Commit(target, AnimationName, length: Duration,
                finished: (_, cancelled) => completion.TrySetResult(cancelled));

            return completion.Task;
        }

        void ApplyColor(bool complete)
        {
            var key = complete ? CompletedColorKey : DefaultColorKey;
            if (ring is not null && key is not null)
                ring.SetDynamicResource(Ring.ProgressColorProperty, key);
        }

        async Task PulseAsync()
        {
            VisualElement? target = PulseTarget ?? ring;
            if (target is null) return;

            await target.ScaleToAsync(1.04, 120, Easing.CubicOut);
            await target.ScaleToAsync(1.0, 180, Easing.SpringOut);
        }
    }
}