using Syncfusion.Maui.Toolkit.ProgressBar;

namespace FitISO.Maui.Behaviors
{
    public class AnimatedSegmentedProgressBehavior : Behavior<SfLinearProgressBar>
    {
        public static readonly BindableProperty CompletedSetsProperty =
            BindableProperty.Create(
                nameof(CompletedSets), typeof(int), typeof(AnimatedSegmentedProgressBehavior), 0,
                propertyChanged: (b, _, _) => ((AnimatedSegmentedProgressBehavior)b).Update());

        public static readonly BindableProperty TotalSetsProperty =
            BindableProperty.Create(
                nameof(TotalSets), typeof(int), typeof(AnimatedSegmentedProgressBehavior), 0,
                propertyChanged: (b, _, _) => ((AnimatedSegmentedProgressBehavior)b).Update());

        public static readonly BindableProperty PulseTargetProperty =
            BindableProperty.Create(
                nameof(PulseTarget), typeof(VisualElement), typeof(AnimatedSegmentedProgressBehavior));

        public int CompletedSets
        {
            get => (int)GetValue(CompletedSetsProperty);
            set => SetValue(CompletedSetsProperty, value);
        }

        public int TotalSets
        {
            get => (int)GetValue(TotalSetsProperty);
            set => SetValue(TotalSetsProperty, value);
        }

        public VisualElement? PulseTarget
        {
            get => (VisualElement?)GetValue(PulseTargetProperty);
            set => SetValue(PulseTargetProperty, value);
        }

        public string? DefaultColorKey { get; set; }
        public string? CompletedColorKey { get; set; }

        SfLinearProgressBar? bar;
        bool wasComplete;
        int version;

        bool IsComplete => TotalSets > 0 && CompletedSets >= TotalSets;

        protected override void OnAttachedTo(SfLinearProgressBar bindable)
        {
            base.OnAttachedTo(bindable);
            bar = bindable;

            BindingContext = bindable.BindingContext;
            bindable.BindingContextChanged += OnBarBindingContextChanged;

            wasComplete = IsComplete;
            ApplyColor(wasComplete);
        }

        protected override void OnDetachingFrom(SfLinearProgressBar bindable)
        {
            bindable.BindingContextChanged -= OnBarBindingContextChanged;
            version++;
            bar = null;
            base.OnDetachingFrom(bindable);
        }

        void OnBarBindingContextChanged(object? sender, EventArgs e)
            => BindingContext = bar?.BindingContext;

        async void Update()
        {
            if (bar is null) return;

            int ticket = ++version;

            if (!IsComplete)
            {
                wasComplete = false;
                ApplyColor(false);
                return;
            }

            if (wasComplete) return;
            wasComplete = true;

            if (!bar.IsLoaded)
            {
                ApplyColor(true);
                return;
            }

            await Task.Delay((int)bar.AnimationDuration);
            if (bar is null || ticket != version) return;

            ApplyColor(true);
            await PulseAsync();
        }

        void ApplyColor(bool complete)
        {
            var key = complete ? CompletedColorKey : DefaultColorKey;
            if (bar is not null && key is not null)
                bar.SetDynamicResource(SfLinearProgressBar.ProgressFillProperty, key);
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