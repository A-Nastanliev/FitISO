using FitISO.Maui.Rendering;
using FitISO.Maui.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using System.ComponentModel;

namespace FitISO.Maui.Views;

public partial class HistoryPage : ContentPage
{
    readonly HistoryPageViewModel viewModel;

    public HistoryPage(HistoryPageViewModel historyPageViewModel)
    {
        InitializeComponent();
        BindingContext = historyPageViewModel;
        viewModel = historyPageViewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.LoadFirst();
        await viewModel.RefreshHeatmapIfStaleAsync();
    }

    void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(HistoryPageViewModel.HeatmapWorkoutDays):
            case nameof(HistoryPageViewModel.HeatmapToday):
            case nameof(HistoryPageViewModel.HeatmapDaysInMonth):
            case nameof(HistoryPageViewModel.HeatmapFirstDayOfWeek):
                HeatmapCanvas.InvalidateSurface();
                break;
        }
    }

    void OnHeatmapPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var (workoutColor, restColor, futureColor) = ResolveHeatmapColors();

        HeatmapChartDrawer.Draw(canvas, viewModel.HeatmapWorkoutDays, viewModel.HeatmapToday,  viewModel.HeatmapDaysInMonth,
            viewModel.HeatmapFirstDayOfWeek, workoutColor, restColor, futureColor, e.Info.Width, e.Info.Height);
    }

    static (SKColor workout, SKColor rest, SKColor future) ResolveHeatmapColors()
    {
        SKColor Get(string key) => ((Color)Application.Current!.Resources[key]).ToSKColor();
        return (Get("ChartAccentColor"), Get("Gray700"), Get("Gray900"));
    }
}