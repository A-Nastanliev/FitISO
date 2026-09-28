using FitISO.Maui.Views;
using FitISO.Maui.Models;

namespace FitISO.Maui.Behaviors
{
    public class SetCompleteBehavior : Behavior<VisualElement>
    {
        public static readonly BindableProperty PunchTargetProperty =
            BindableProperty.Create(nameof(PunchTarget), typeof(VisualElement), typeof(SetCompleteBehavior));

        public static readonly BindableProperty RippleProperty =
            BindableProperty.Create(nameof(Ripple), typeof(SetRippleView), typeof(SetCompleteBehavior));

        public VisualElement? PunchTarget
        {
            get => (VisualElement?)GetValue(PunchTargetProperty);
            set => SetValue(PunchTargetProperty, value);
        }

        public SetRippleView? Ripple
        {
            get => (SetRippleView?)GetValue(RippleProperty);
            set => SetValue(RippleProperty, value);
        }

        public double PunchScale { get; set; } = 1.06;
        public string ColorKey { get; set; } = "ChartAccentColor";

        VisualElement? view;
        Set? currentSet;

        protected override void OnAttachedTo(VisualElement bindable)
        {
            base.OnAttachedTo(bindable);
            view = bindable;
            bindable.BindingContextChanged += OnViewBindingContextChanged;
            Hook(bindable.BindingContext as Set);
        }

        protected override void OnDetachingFrom(VisualElement bindable)
        {
            bindable.BindingContextChanged -= OnViewBindingContextChanged;
            Hook(null);
            ResetVisuals();
            view = null;
            base.OnDetachingFrom(bindable);
        }

        void OnViewBindingContextChanged(object? sender, EventArgs e)
        {
            ResetVisuals();
            Hook(view?.BindingContext as Set);
        }

        void Hook(Set? set)
        {
            if (currentSet is not null)
                currentSet.JustCompleted -= OnJustCompleted;

            currentSet = set;

            if (currentSet is not null)
                currentSet.JustCompleted += OnJustCompleted;
        }

        void OnJustCompleted(object? sender, EventArgs e)
        {

            view?.Dispatcher.Dispatch(() => _ = PlayAsync());
        }

        void ResetVisuals()
        {
            if (PunchTarget is { } target)
            {
                target.CancelAnimations();
                target.Scale = 1;
            }
            Ripple?.Cancel();
        }

        async Task PlayAsync()
        {
            if (view is null || !view.IsLoaded || PunchTarget is not { } target)
                return;

            target.CancelAnimations();
            target.Scale = 1;

            await Task.WhenAll(PunchAsync(target), Ripple?.PlayAsync(ResolveColor()) ?? Task.CompletedTask);
        }

        async Task PunchAsync(VisualElement target)
        {
            if (await target.ScaleToAsync(PunchScale, 90, Easing.CubicOut))
                return;
            await target.ScaleToAsync(1.0, 260, Easing.SpringOut);
        }

        Color ResolveColor()
        {
            if (Application.Current?.Resources.TryGetValue(ColorKey, out var value) == true && value is Color color)
                return color;

            return Color.FromRgb(205, 92, 92);
        }
    }
}