namespace FitISO.Maui.Behaviors
{
    public class AnimatedProgressBehavior : Behavior<ProgressBar>
    {
        public static readonly BindableProperty TargetProgressProperty =
            BindableProperty.Create(
                nameof(TargetProgress), typeof(double), typeof(AnimatedProgressBehavior), 0d,
                propertyChanged: (b, _, n) => ((AnimatedProgressBehavior)b).Animate((double)n));

        public static readonly BindableProperty PulseTargetProperty =
            BindableProperty.Create(
                nameof(PulseTarget), typeof(VisualElement), typeof(AnimatedProgressBehavior));

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

        ProgressBar? bar;
        bool wasComplete;

        protected override void OnAttachedTo(ProgressBar bindable)
        {
            base.OnAttachedTo(bindable);
            bar = bindable;

            BindingContext = bindable.BindingContext;
            bindable.BindingContextChanged += OnBarBindingContextChanged;

            bindable.Progress = TargetProgress;
            ApplyColor(TargetProgress >= 1);
        }

        protected override void OnDetachingFrom(ProgressBar bindable)
        {
            bindable.BindingContextChanged -= OnBarBindingContextChanged;
            bindable.AbortAnimation("Progress");
            bar = null;
            base.OnDetachingFrom(bindable);
        }

        void OnBarBindingContextChanged(object? sender, EventArgs e)
            => BindingContext = bar?.BindingContext;

        async void Animate(double value)
        {
            if (bar is null) return;
            bool complete = value >= 1;

            if (!bar.IsLoaded)
            {
                bar.Progress = value;
                ApplyColor(complete);
                wasComplete = complete;
                return;
            }

            bool cancelled = await bar.ProgressTo(value, Duration, Easing.CubicOut);
            if (cancelled || bar is null) return;   

            ApplyColor(complete);                   

            if (complete && !wasComplete)
                await PulseAsync();

            wasComplete = complete;
        }

        void ApplyColor(bool complete)
        {
            var key = complete ? CompletedColorKey : DefaultColorKey;
            if (bar is not null && key is not null)
                bar.SetDynamicResource(ProgressBar.ProgressColorProperty, key);
        }

        async Task PulseAsync()
        {
            VisualElement? target = PulseTarget ?? bar;
            if (target is null) return;

            await target.ScaleToAsync(1.04, 120, Easing.CubicOut);
            await target.ScaleToAsync(1.0, 180, Easing.SpringOut);
        }
    }
}
