using FitISO.Maui.Rendering;
using FitISO.Maui.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using System.ComponentModel;

namespace FitISO.Maui.Views
{
    public partial class HeatmapView : ContentView
    {
        const double CommitThresholdFraction = 0.28;
        const double RubberBandLimit = 24;
        const double UncachedDragCapFraction = 0.15;

        HeatmapViewModel? heatmapViewModel;
        int panDirection;
        int previewYear, previewMonth;
        HashSet<int>? previewDays;
        bool committing;

        public HeatmapView()
        {
            InitializeComponent();
            Unloaded += OnUnloaded;
        }

        public void Attach(HeatmapViewModel viewModel)
        {
            if (heatmapViewModel is not null)
                heatmapViewModel.PropertyChanged -= OnHeatmapViewModelPropertyChanged;

            heatmapViewModel = viewModel;
            BindingContext = viewModel;
            heatmapViewModel.PropertyChanged += OnHeatmapViewModelPropertyChanged;
        }

        public Task RefreshIfStaleAsync() =>
            heatmapViewModel?.RefreshHeatmapIfStaleAsync() ?? Task.CompletedTask;

        void OnUnloaded(object? sender, EventArgs e)
        {
            if (heatmapViewModel is not null)
                heatmapViewModel.PropertyChanged -= OnHeatmapViewModelPropertyChanged;
        }

        void OnHeatmapViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(HeatmapViewModel.HeatmapWorkoutDays):
                case nameof(HeatmapViewModel.HeatmapToday):
                case nameof(HeatmapViewModel.HeatmapDaysInMonth):
                case nameof(HeatmapViewModel.HeatmapFirstDayOfWeek):
                case nameof(HeatmapViewModel.HeatmapWeekStartDay):
                case nameof(HeatmapViewModel.HeatmapUseGitHubStyleLayout):
                    HeatmapCanvas.InvalidateSurface();
                    break;
            }
        }

        void OnHeatmapPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
        {
            if (heatmapViewModel is null)
                return;

            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            var (workoutColor, restColor, futureColor) = ResolveHeatmapColors();

            HeatmapChartDrawer.Draw(canvas, heatmapViewModel.HeatmapWorkoutDays, heatmapViewModel.HeatmapToday, heatmapViewModel.HeatmapDaysInMonth,
                  heatmapViewModel.HeatmapFirstDayOfWeek, heatmapViewModel.HeatmapWeekStartDay, heatmapViewModel.HeatmapUseGitHubStyleLayout,
                  workoutColor, restColor, futureColor, e.Info.Width, e.Info.Height);
        }

        static (SKColor workout, SKColor rest, SKColor future) ResolveHeatmapColors()
        {
            SKColor Get(string key) => ((Color)Application.Current!.Resources[key]).ToSKColor();
            return (Get("ChartAccentColor"), Get("Gray700"), Get("Gray900"));
        }

        void OnHeatmapPreviewPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
        {
            if (heatmapViewModel is null || previewDays is not { } days)
                return;

            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            var now = DateTime.Now;
            var isCurrentMonth = previewYear == now.Year && previewMonth == now.Month;
            var daysInMonth = DateTime.DaysInMonth(previewYear, previewMonth);
            var today = isCurrentMonth ? now.Day : daysInMonth;
            var firstDayOfWeek = new DateTime(previewYear, previewMonth, 1).DayOfWeek;

            var (workoutColor, restColor, futureColor) = ResolveHeatmapColors();

            HeatmapChartDrawer.Draw(canvas, days, today, daysInMonth,
                firstDayOfWeek, heatmapViewModel.HeatmapWeekStartDay, heatmapViewModel.HeatmapUseGitHubStyleLayout,
                workoutColor, restColor, futureColor, e.Info.Width, e.Info.Height);
        }

        async void OnHeatmapPanUpdated(object? sender, PanUpdatedEventArgs e)
        {
            if (committing)
                return;

            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    OnPanStarted();
                    break;
                case GestureStatus.Running:
                    OnPanRunning(e.TotalX);
                    break;
                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    await OnPanEndedAsync();
                    break;
            }
        }

        void OnPanStarted()
        {
            HeatmapCanvas.CancelAnimations();
            HeatmapPreviewCanvas.CancelAnimations();
            panDirection = 0;
            previewDays = null;
            HeatmapPreviewCanvas.IsVisible = false;
            HeatmapPreviewCanvas.Opacity = 0;
        }

        void OnPanRunning(double totalX)
        {
            if (heatmapViewModel is null)
                return;

            var width = HeatmapSwipeContainer.Width;
            if (width <= 0)
                return;

            var direction = totalX < 0 ? 1 : -1;
            var canGo = direction == 1 ? heatmapViewModel.CanGoNextMonth : heatmapViewModel.CanGoPreviousMonth;

            if (!canGo)
            {
                HeatmapPreviewCanvas.IsVisible = false;
                HeatmapCanvas.TranslationX = Math.Sign(totalX) * Math.Min(Math.Abs(totalX) * 0.35, RubberBandLimit);
                return;
            }

            if (panDirection != direction)
            {
                panDirection = direction;
                var (year, month) = TargetMonth(direction);
                previewYear = year;
                previewMonth = month;
                previewDays = heatmapViewModel.TryGetCachedDays(year, month, out var cached) ? cached : null;
                HeatmapPreviewCanvas.IsVisible = previewDays is not null;
                HeatmapPreviewCanvas.InvalidateSurface();
            }

            if (previewDays is null)
            {
                HeatmapCanvas.TranslationX = Math.Sign(totalX) * Math.Min(Math.Abs(totalX), width * UncachedDragCapFraction);
                return;
            }

            var dx = Math.Sign(totalX) * Math.Min(Math.Abs(totalX), width);
            HeatmapCanvas.TranslationX = dx;
            HeatmapPreviewCanvas.TranslationX = direction * width + dx;
            HeatmapPreviewCanvas.Opacity = Math.Min(1, Math.Abs(dx) / (width * 0.5));
        }

        async Task OnPanEndedAsync()
        {
            if (heatmapViewModel is null)
            {
                ResetPanVisualState();
                return;
            }

            var width = HeatmapSwipeContainer.Width;
            var dx = HeatmapCanvas.TranslationX;
            var threshold = width * CommitThresholdFraction;

            var shouldCommit = panDirection != 0 && previewDays is not null && width > 0 && Math.Abs(dx) >= threshold;

            if (!shouldCommit)
            {
                await SnapBackAsync();
                return;
            }

            committing = true;
            try
            {
                await CommitPanAsync(panDirection, width);
            }
            finally
            {
                committing = false;
            }
        }

        async Task SnapBackAsync()
        {
            var reset = Task.WhenAll(
                HeatmapCanvas.TranslateToAsync(0, 0, 220, Easing.SpringOut),
                HeatmapPreviewCanvas.FadeToAsync(0, 160, Easing.CubicOut));

            await reset;
            ResetPanVisualState();
        }

        async Task CommitPanAsync(int direction, double width)
        {
            HeatmapCanvasSlideBehavior.SkipNextChange();

            await Task.WhenAll(
                HeatmapCanvas.TranslateToAsync(-direction * width, 0, 220, Easing.CubicOut),
                HeatmapPreviewCanvas.TranslateToAsync(0, 0, 220, Easing.CubicOut),
                HeatmapPreviewCanvas.FadeToAsync(1, 220, Easing.CubicOut));

            var command = direction == 1
                ? heatmapViewModel!.HeatmapNextMonthCommand
                : heatmapViewModel!.HeatmapPreviousMonthCommand;

            if (command.CanExecute(null))
                await command.ExecuteAsync(null);

            ResetPanVisualState();
        }

        void ResetPanVisualState()
        {
            HeatmapCanvas.TranslationX = 0;
            HeatmapCanvas.Opacity = 1;
            HeatmapPreviewCanvas.IsVisible = false;
            HeatmapPreviewCanvas.TranslationX = 0;
            HeatmapPreviewCanvas.Opacity = 0;
            panDirection = 0;
            previewDays = null;
        }

        (int Year, int Month) TargetMonth(int direction)
        {
            var (year, month) = heatmapViewModel!.CurrentYearMonth;
            var target = new DateTime(year, month, 1).AddMonths(direction);
            return (target.Year, target.Month);
        }
    }
}