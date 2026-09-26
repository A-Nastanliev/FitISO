using FitISO.Maui.Rendering;
using FitISO.Maui.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using System.ComponentModel;

namespace FitISO.Maui.Views;

public partial class HeatmapView : ContentView
{
    HeatmapViewModel? heatmapViewModel;

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
}
