using FitISO.Maui.Models;
using SkiaSharp;

namespace FitISO.Maui.Rendering
{
    public static class ExerciseProgressPdfBuilder
    {
        const double RepsInfluence = 0.01;
        const double MaxRepsOffset = 0.5;

        const float PageWidth = 842f;
        const float PageHeight = 595f;
        const float Margin = 40f;
        const float YAxisWidth = 40f;
        const float HeaderHeight = 80f;
        const float XAxisHeight = 36f;
        const int PointsPerPage = 12;
        const int GridRows = 4;

        static SKColor ResolveColor(string resourceKey, SKColor fallback)
        {
            if (Application.Current?.Resources.TryGetValue(resourceKey, out var value) == true
                && value is Color color)
            {
                return new SKColor(
                    (byte)(color.Red * 255),
                    (byte)(color.Green * 255),
                    (byte)(color.Blue * 255));
            }

            return fallback;
        }

        static double WeightWithRepsTiebreak(double weight, double reps) =>
            weight + Math.Min(reps * RepsInfluence, MaxRepsOffset);

        public static void Build(Exercise exercise, Stream output)
        {
            var history = exercise.History;
            if (history.Count == 0)
                return;

            using var document = SKDocument.CreatePdf(output);

            using var boldTypeface = SKTypeface.FromFamilyName(null, SKFontStyle.Bold);
            using var titleFont = new SKFont(boldTypeface, 22);
            using var subFont = new SKFont(SKTypeface.Default, 12);
            using var labelFont = new SKFont(SKTypeface.Default, 11);

            var accent = ResolveColor("ChartAccentColor", new SKColor(205, 92, 92));

            using var blackPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            using var grayPaint = new SKPaint { Color = new SKColor(90, 90, 90), IsAntialias = true };
            using var gridPaint = new SKPaint { Color = new SKColor(210, 210, 210), StrokeWidth = 1, Style = SKPaintStyle.Stroke };
            using var fillPaint = new SKPaint { Color = accent.WithAlpha(50), IsAntialias = true, Style = SKPaintStyle.Fill };
            using var linePaint = new SKPaint { Color = accent, StrokeWidth = 2, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeJoin = SKStrokeJoin.Round };
            using var dotFillPaint = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Fill };
            using var dotStrokePaint = new SKPaint { Color = accent, StrokeWidth = 2, IsAntialias = true, Style = SKPaintStyle.Stroke };

            var values = history.Select(h => WeightWithRepsTiebreak(h.Weight, h.Reps)).ToList();
            var min = (float)values.Min();
            var max = (float)values.Max();
            if (Math.Abs(max - min) < 0.001f)
            {
                min -= 1;
                max += 1;
            }
            else
            {
                var pad = (max - min) * 0.1f;
                min -= pad;
                max += pad;
            }

            var best = history.OrderByDescending(h => h.Weight).ThenByDescending(h => h.Reps).First();
            var pageCount = (int)Math.Ceiling(history.Count / (double)PointsPerPage);

            var plotLeft = Margin + YAxisWidth;
            var plotRight = PageWidth - Margin;
            var plotTop = Margin + HeaderHeight;
            var plotBottom = PageHeight - Margin - XAxisHeight;
            var plotHeight = plotBottom - plotTop;
            var spacing = (plotRight - plotLeft) / PointsPerPage;

            for (var page = 0; page < pageCount; page++)
            {
                var canvas = document.BeginPage(PageWidth, PageHeight);

                canvas.DrawText(exercise.Name, Margin, Margin + 20, SKTextAlign.Left, titleFont, blackPaint);
                canvas.DrawText($"PR: {best.Weight:0.##} kg × {best.Reps:0.##}", Margin, Margin + 42, SKTextAlign.Left, subFont, grayPaint);
                canvas.DrawText($"{history[0].Date:MMM d, yyyy} – {history[^1].Date:MMM d, yyyy}  ·  page {page + 1}/{pageCount}",
                    Margin, Margin + 60, SKTextAlign.Left, subFont, grayPaint);

                for (var i = 0; i <= GridRows; i++)
                {
                    var y = plotTop + plotHeight * i / GridRows;
                    var value = max - (max - min) * i / GridRows;
                    canvas.DrawLine(plotLeft, y, plotRight, y, gridPaint);
                    canvas.DrawText(value.ToString("0.#"), plotLeft - 6, y + labelFont.Size * 0.35f,
                        SKTextAlign.Right, labelFont, grayPaint);
                }

                var startIndex = page * PointsPerPage;
                var count = Math.Min(PointsPerPage, history.Count - startIndex);
                var points = new SKPoint[count];

                for (var i = 0; i < count; i++)
                {
                    var x = plotLeft + spacing * (i + 0.5f);
                    var normalized = (float)((values[startIndex + i] - min) / (max - min));
                    points[i] = new SKPoint(x, plotBottom - normalized * plotHeight);
                }

                if (count > 1)
                {
                    using var fillPath = new SKPath();
                    fillPath.MoveTo(points[0].X, plotBottom);
                    foreach (var p in points)
                        fillPath.LineTo(p.X, p.Y);
                    fillPath.LineTo(points[^1].X, plotBottom);
                    fillPath.Close();
                    canvas.DrawPath(fillPath, fillPaint);

                    using var linePath = new SKPath();
                    linePath.MoveTo(points[0].X, points[0].Y);
                    for (var i = 1; i < count; i++)
                        linePath.LineTo(points[i].X, points[i].Y);
                    canvas.DrawPath(linePath, linePaint);
                }

                for (var i = 0; i < count; i++)
                {
                    var h = history[startIndex + i];
                    var p = points[i];

                    canvas.DrawCircle(p, 4, dotFillPaint);
                    canvas.DrawCircle(p, 4, dotStrokePaint);
                    canvas.DrawText($"{h.Weight:0.##}×{h.Reps:0.##}", p.X, p.Y - 10, SKTextAlign.Center, labelFont, blackPaint);
                    canvas.DrawText(h.Date.ToString("MMM d"), p.X, plotBottom + 16, SKTextAlign.Center, labelFont, blackPaint);
                    canvas.DrawText(h.Date.Year.ToString(), p.X, plotBottom + 29, SKTextAlign.Center, labelFont, grayPaint);
                }

                document.EndPage();
            }

            document.Close();
        }
    }
}