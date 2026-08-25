using SkiaSharp;
using System;
using FitISO.Maui.Models;
using System.Collections.Generic;
using System.Linq;

namespace FitISO.Maui.Platforms.Android
{
    public static class ExerciseChartDrawer
    {
        static readonly SKColor AccentColor = new SKColor(205, 92, 92);

        public static SKBitmap Draw(IReadOnlyList<ExerciseHistoryPoint> history, int width, int height)
        {
            var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.Transparent);

            if (history.Count == 0)
                return bitmap;

            var axisTextSize = Math.Clamp(height * 0.10f, 20f, 34f);
            using var axisFont = new SKFont
            {
                Size = axisTextSize,
                Typeface = SKTypeface.FromFamilyName(null, SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
            };
            using var axisPaint = new SKPaint
            {
                Color = SKColors.White.WithAlpha(230),
                IsAntialias = true
            };

            var xAxisHeight = axisTextSize + 8f;
            var topPadding = axisFont.Metrics.CapHeight + 8f;

            var lastLabelWidth = axisFont.MeasureText(history[^1].Date.ToString("MMM d"));
            var rightPadding = lastLabelWidth / 2f + 6f;

            var values = history
                .Select(h => ExerciseChartMath.WeightWithRepsTiebreak(h.Weight, h.Reps))
                .ToList();

            var min = (float)values.Min();
            var max = (float)values.Max();
            if (Math.Abs(max - min) < 0.001f)
            {
                max += 1;
                min -= 1;
            }

            var mid = (min + max) / 2f;
            var yAxisLabelWidth = new[] { max, mid, min }
                .Select(v => axisFont.MeasureText(v.ToString("0.#")))
                .Max();
            var yAxisWidth = yAxisLabelWidth + 6f;

            var plotLeft = yAxisWidth;
            var plotRight = width - rightPadding;
            var plotTop = topPadding;
            var plotBottom = height - xAxisHeight;
            var plotWidth = Math.Max(plotRight - plotLeft, 1f);
            var plotHeight = Math.Max(plotBottom - plotTop, 1f);

            var points = new SKPoint[history.Count];
            for (var i = 0; i < history.Count; i++)
            {
                var x = history.Count == 1
                    ? plotLeft + plotWidth / 2f
                    : plotLeft + plotWidth * i / (history.Count - 1);

                var normalized = (float)((values[i] - min) / (max - min));
                var y = plotBottom - normalized * plotHeight;
                points[i] = new SKPoint(x, y);
            }

            using var gridPaint = new SKPaint
            {
                Color = SKColors.White.WithAlpha(30),
                StrokeWidth = 1,
                IsAntialias = false,
                Style = SKPaintStyle.Stroke
            };

            const int RowCount = 4;
            for (var i = 0; i <= RowCount; i++)
            {
                var y = plotTop + plotHeight * i / RowCount;
                canvas.DrawLine(plotLeft, y, plotRight, y, gridPaint);
            }

            using var fillPaint = new SKPaint
            {
                Color = AccentColor.WithAlpha(60),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            using var linePaint = new SKPaint
            {
                Color = AccentColor,
                StrokeWidth = 3,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round
            };

            using var dotFillPaint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            using var dotStrokePaint = new SKPaint
            {
                Color = AccentColor,
                StrokeWidth = 3,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke
            };

            if (points.Length > 1)
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
                for (var i = 1; i < points.Length; i++)
                    linePath.LineTo(points[i].X, points[i].Y);
                canvas.DrawPath(linePath, linePaint);
            }

            foreach (var p in points)
            {
                canvas.DrawCircle(p, 5, dotFillPaint);
                canvas.DrawCircle(p, 5, dotStrokePaint);
            }

            DrawYAxisLabels(canvas, axisFont, axisPaint, min, max, plotTop, plotBottom, yAxisWidth);
            DrawXAxisLabels(canvas, axisFont, axisPaint, history, points, plotBottom, xAxisHeight, width);

            return bitmap;
        }

        static void DrawYAxisLabels(SKCanvas canvas, SKFont font, SKPaint paint, float min, float max, float plotTop, float plotBottom, float yAxisWidth)
        {
            var mid = (min + max) / 2f;
            var ticks = new[] { max, mid, min };
            var ys = new[] { plotTop, (plotTop + plotBottom) / 2f, plotBottom };
            var metrics = font.Metrics;

            for (var i = 0; i < ticks.Length; i++)
            {
                var text = ticks[i].ToString("0.#");
                var textWidth = font.MeasureText(text);
                var textY = ys[i] - (metrics.Ascent + metrics.Descent) / 2f;
                canvas.DrawText(text, yAxisWidth - textWidth - 6f, textY, font, paint);
            }
        }

        static void DrawXAxisLabels(SKCanvas canvas, SKFont font, SKPaint paint, IReadOnlyList<ExerciseHistoryPoint> history, SKPoint[] points, float plotBottom, float xAxisHeight, float canvasWidth)
        {
            var labelCount = Math.Clamp(history.Count, 2, 4);
            var indices = PickIndices(history.Count, labelCount);
            var textY = plotBottom + xAxisHeight - 4f;

            foreach (var i in indices)
            {
                var text = history[i].Date.ToString("MMM d");
                var textWidth = font.MeasureText(text);
                var textX = Math.Clamp(points[i].X - textWidth / 2f, 0f, canvasWidth - textWidth);
                canvas.DrawText(text, textX, textY, font, paint);
            }
        }

        static int[] PickIndices(int count, int labelCount)
        {
            if (count <= labelCount)
                return Enumerable.Range(0, count).ToArray();

            var indices = new int[labelCount];
            for (var i = 0; i < labelCount; i++)
                indices[i] = (int)Math.Round(i * (count - 1) / (double)(labelCount - 1));

            return indices.Distinct().ToArray();
        }
    }
}