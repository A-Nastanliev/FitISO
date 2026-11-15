using SkiaSharp;
using System;
using FitISO.Maui.Models;
using System.Collections.Generic;
using System.Linq;

namespace FitISO.Maui.Rendering
{
    public static class ExerciseChartDrawer
    {
        const double RepsInfluence = 0.01;
        const double MaxRepsOffset = 0.5;

        const float AxisTextDp = 11f;

        public static SKBitmap Draw(IReadOnlyList<ExerciseHistoryPoint> history, int width, int height,
            SKColor accent, SKColor gridColor, float density = 0f)
        {
            return SkiaDrawer.CreateTransparentBitmap(width, height, canvas =>
                Draw(canvas, history, width, height, accent, gridColor, density));
        }

        public static void Draw(SKCanvas canvas, IReadOnlyList<ExerciseHistoryPoint> history, int width, int height,
            SKColor accent, SKColor gridColor, float density = 0f)
        {
            if (history.Count == 0)
                return;

            float Size(float legacyPx, float dp) => density > 0f ? dp * density : legacyPx;

            var axisTextSize = density > 0f
                ? AxisTextDp * density
                : Math.Clamp(height * 0.10f, 20f, 34f);

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

            var capHeight = axisFont.Metrics.CapHeight;
            var descent = axisFont.Metrics.Descent;

            var yLabelLift = Size(4f, 2f);
            var yLabelGap = Size(6f, 4f);
            var xLabelTopGap = Size(10f, 6f);

            var topPadding = capHeight + yLabelLift + Size(2f, 1f);
            var xAxisHeight = xLabelTopGap + capHeight + descent + Size(2f, 1f);

            var lastLabelWidth = axisFont.MeasureText(history[^1].Date.ToString("MMM d"));
            var rightPadding = Math.Max(lastLabelWidth / 2f + Size(2f, 1f), Size(8f, 5f));

            var values = history
                .Select(h => WeightWithRepsTiebreak(h.Weight, h.Reps))
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
            var yAxisWidth = yAxisLabelWidth + yLabelGap;

            var firstLabelWidth = axisFont.MeasureText(history[0].Date.ToString("MMM d"));
            var plotInset = Size(14f, 8f);
            var plotLeft = Math.Max(yAxisWidth + plotInset, firstLabelWidth / 2f + Size(5f, 3f));
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
                Color = gridColor.WithAlpha(70),
                StrokeWidth = Math.Max(1f, Size(1f, 0.5f)),
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
                Color = accent.WithAlpha(60),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            using var linePaint = new SKPaint
            {
                Color = accent,
                StrokeWidth = Size(3f, 2f),
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
                Color = accent,
                StrokeWidth = Size(3f, 1.5f),
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

            var dotRadius = Size(5f, 3f);
            foreach (var p in points)
            {
                canvas.DrawCircle(p, dotRadius, dotFillPaint);
                canvas.DrawCircle(p, dotRadius, dotStrokePaint);
            }

            DrawYAxisLabels(canvas, axisFont, axisPaint, min, max, plotTop, plotBottom, yAxisWidth, yLabelGap, yLabelLift);

            var xLabelBaseline = plotBottom + xLabelTopGap + capHeight;
            var minLabelGap = Size(8f, 4f);
            var comfortLabelGap = Size(36f, 18f);
            DrawXAxisLabels(canvas, axisFont, axisPaint, history, points, xLabelBaseline, minLabelGap, comfortLabelGap);
        }

        static double WeightWithRepsTiebreak(double weight, double reps) =>
            weight + Math.Min(reps * RepsInfluence, MaxRepsOffset);

        static void DrawYAxisLabels(SKCanvas canvas, SKFont font, SKPaint paint, float min, float max, float plotTop, float plotBottom, float yAxisWidth, float gap, float lift)
        {
            var mid = (min + max) / 2f;
            var ticks = new[] { max, mid, min };
            var ys = new[] { plotTop, (plotTop + plotBottom) / 2f, plotBottom };

            for (var i = 0; i < ticks.Length; i++)
            {
                var text = ticks[i].ToString("0.#");
                var textWidth = font.MeasureText(text);

                canvas.DrawText(text, yAxisWidth - textWidth - gap, ys[i] - lift, font, paint);
            }
        }

        static void DrawXAxisLabels(SKCanvas canvas, SKFont font, SKPaint paint, IReadOnlyList<ExerciseHistoryPoint> history, SKPoint[] points, float baselineY, float minGap, float comfortGap)
        {
            foreach (var (text, centerX) in ChooseLabels(font, history, points, minGap, comfortGap))
            {
                var textWidth = font.MeasureText(text);
                canvas.DrawText(text, centerX - textWidth / 2f, baselineY, font, paint);
            }
        }

        static List<(string Text, float CenterX)> ChooseLabels(SKFont font, IReadOnlyList<ExerciseHistoryPoint> history, SKPoint[] points, float minGap, float comfortGap)
        {
            var count = history.Count;
            if (count == 1)
                return BuildLabels(history, points, 1);

            var maxLabels = FiveLabelsFitComfortably(font, history, points, comfortGap) ? 5 : 3;

            for (var labelCount = Math.Min(maxLabels, count); labelCount >= 2; labelCount--)
            {
                var labels = BuildLabels(history, points, labelCount);
                if (LabelsFit(font, labels, minGap))
                    return labels;
            }

            return BuildLabels(history, points, 2);
        }

        static bool FiveLabelsFitComfortably(SKFont font, IReadOnlyList<ExerciseHistoryPoint> history, SKPoint[] points, float comfortGap)
        {
            var widestLabel = history.Max(h => font.MeasureText(h.Date.ToString("MMM d")));

            var spacing = (points[^1].X - points[0].X) / 4f;

            return spacing >= widestLabel + comfortGap;
        }

        static List<(string Text, float CenterX)> BuildLabels(IReadOnlyList<ExerciseHistoryPoint> history, SKPoint[] points, int labelCount)
        {
            var count = history.Count;
            var labels = new List<(string Text, float CenterX)>(labelCount);

            if (labelCount == 1)
            {
                labels.Add((history[0].Date.ToString("MMM d"), points[0].X));
                return labels;
            }

            for (var k = 0; k < labelCount; k++)
            {
                var position = (count - 1) * (double)k / (labelCount - 1);
                var lower = (int)Math.Floor(position);
                var upper = Math.Min(lower + 1, count - 1);
                var fraction = (float)(position - lower);

                var centerX = points[lower].X + (points[upper].X - points[lower].X) * fraction;
                var dateIndex = (int)Math.Round(position, MidpointRounding.AwayFromZero);

                labels.Add((history[dateIndex].Date.ToString("MMM d"), centerX));
            }

            return labels;
        }

        static bool LabelsFit(SKFont font, List<(string Text, float CenterX)> labels, float minGap)
        {
            var previousRight = float.NegativeInfinity;

            foreach (var (text, centerX) in labels)
            {
                var left = centerX - font.MeasureText(text) / 2f;
                if (left < previousRight + minGap)
                    return false;

                previousRight = left + font.MeasureText(text);
            }

            return true;
        }
    }
}