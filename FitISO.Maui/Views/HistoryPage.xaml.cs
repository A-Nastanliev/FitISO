using FitISO.Maui.Rendering;
using FitISO.Maui.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace FitISO.Maui.Views;

public partial class HistoryPage : ContentPage
{
    readonly HistoryPageViewModel viewModel;

    public HistoryPage(HistoryPageViewModel historyPageViewModel)
    {
        InitializeComponent();
        BindingContext = historyPageViewModel;
        viewModel = historyPageViewModel;
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.LoadFirst();
        await viewModel.RefreshHeatmapIfStaleAsync();
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