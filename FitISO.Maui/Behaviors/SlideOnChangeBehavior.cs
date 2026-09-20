namespace FitISO.Maui.Behaviors
{
    public class SlideOnChangeBehavior : Behavior<VisualElement>
    {
        public static readonly BindableProperty ValueProperty =
            BindableProperty.Create(
                nameof(Value), typeof(int), typeof(SlideOnChangeBehavior), 0,
                propertyChanged: (b, o, n) => ((SlideOnChangeBehavior)b).OnValueChanged((int)o, (int)n));

        public int Value
        {
            get => (int)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double Distance { get; set; } = 48;
        public uint Duration { get; set; } = 300;

        VisualElement? view;

        protected override void OnAttachedTo(VisualElement bindable)
        {
            base.OnAttachedTo(bindable);
            view = bindable;

            BindingContext = bindable.BindingContext;
            bindable.BindingContextChanged += OnViewBindingContextChanged;
        }

        protected override void OnDetachingFrom(VisualElement bindable)
        {
            bindable.BindingContextChanged -= OnViewBindingContextChanged;
            bindable.CancelAnimations();
            bindable.TranslationX = 0;
            bindable.Opacity = 1;
            view = null;
            base.OnDetachingFrom(bindable);
        }

        void OnViewBindingContextChanged(object? sender, EventArgs e)
            => BindingContext = view?.BindingContext;

        async void OnValueChanged(int oldValue, int newValue)
        {
            if (view is null || !view.IsLoaded || oldValue == 0 || oldValue == newValue) return;

            int direction = newValue > oldValue ? 1 : -1;

            view.CancelAnimations();
            view.TranslationX = direction * Distance;
            view.Opacity = 0;

            await Task.WhenAll(
                view.TranslateToAsync(0, 0, Duration, Easing.CubicOut),
                view.FadeToAsync(1, Duration, Easing.CubicOut));
        }
    }
}