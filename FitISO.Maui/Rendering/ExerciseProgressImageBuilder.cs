using FitISO.Maui.Models;
using SkiaSharp;

namespace FitISO.Maui.Rendering
{
    public static class ExerciseProgressImageBuilder
    {
        const double RepsInfluence = 0.01;
        const double MaxRepsOffset = 0.5;

        const float MinWidth = 1080f;
        const float MaxPlotWidth = 12000f;
        const float OuterMargin = 32f;
        const float CardPadding = 48f;
        const float CardCornerRadius = 72f;
        const float PointSpacing = 130f;
        const float MinLabelSpacing = 100f;
        const float HeaderHeight = 190f;
        const float ChartHeight = 640f;
        const float XAxisHeight = 100f;
        const float YAxisWidth = 90f;
        const float TopPadding = 50f;
        const int GridRows = 4;

        public static void Build(Exercise exercise, Stream output)
        {
            var history = exercise.History;
            if (history.Count == 0)
                return;

            var plotWidth = Math.Min(history.Count * PointSpacing, MaxPlotWidth);
            var cardWidth = Math.Max(MinWidth - OuterMargin * 2, CardPadding * 2 + YAxisWidth + plotWidth);
            var cardHeight = HeaderHeight + ChartHeight + XAxisHeight + 24f;
            var totalWidth = cardWidth + OuterMargin * 2;
            var totalHeight = cardHeight + OuterMargin * 2;

            using var bitmap = new SKBitmap((int)Math.Ceiling(totalWidth), (int)Math.Ceiling(totalHeight));
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.Transparent);

            Render(canvas, exercise, totalWidth, totalHeight);

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            data.SaveTo(output);
        }

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

        static void Render(SKCanvas canvas, Exercise exercise, float totalWidth, float totalHeight)
        {
            var history = exercise.History;
            var accent = ResolveColor("ChartAccentColor", new SKColor(205, 92, 92));

            using var boldTypeface = SKTypeface.FromFamilyName(null, SKFontStyle.Bold);
            using var titleFont = new SKFont(boldTypeface, 48);
            using var subFont = new SKFont(SKTypeface.Default, 30);
            using var labelFont = new SKFont(SKTypeface.Default, 28);
            using var smallFont = new SKFont(SKTypeface.Default, 22);

            using var cardPaint = new SKPaint { Color = ResolveColor("Gray900", new SKColor(0x21, 0x21, 0x21)), IsAntialias = true };
            using var whitePaint = new SKPaint { Color = ResolveColor("White", SKColors.White), IsAntialias = true };
            using var mutedPaint = new SKPaint { Color = ResolveColor("Gray400", new SKColor(0x91, 0x91, 0x91)), IsAntialias = true };
            using var gridPaint = new SKPaint { Color = SKColors.White.WithAlpha(40), StrokeWidth = 2, Style = SKPaintStyle.Stroke };
            using var fillPaint = new SKPaint { Color = accent.WithAlpha(60), IsAntialias = true, Style = SKPaintStyle.Fill };
            using var linePaint = new SKPaint
            {
                Color = accent,
                StrokeWidth = 5,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round
            };
            using var dotFillPaint = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Fill };
            using var dotStrokePaint = new SKPaint { Color = accent, StrokeWidth = 5, IsAntialias = true, Style = SKPaintStyle.Stroke };

            var card = new SKRect(OuterMargin, OuterMargin, totalWidth - OuterMargin, totalHeight - OuterMargin);
            canvas.DrawRoundRect(new SKRoundRect(card, CardCornerRadius), cardPaint);

            var best = history.OrderByDescending(h => h.Weight).ThenByDescending(h => h.Reps).First();
            var textLeft = card.Left + CardPadding;
            canvas.DrawText(exercise.Name, textLeft, card.Top + 80, SKTextAlign.Left, titleFont, whitePaint);
            canvas.DrawText($"PR: {best.Weight:0.##} kg × {best.Reps:0.##}", textLeft, card.Top + 128, SKTextAlign.Left, subFont, whitePaint);
            canvas.DrawText($"{history[0].Date:MMM d, yyyy} – {history[^1].Date:MMM d, yyyy}  ·  {history.Count} workouts",
                textLeft, card.Top + 170, SKTextAlign.Left, subFont, mutedPaint);

            var plotLeft = card.Left + CardPadding + YAxisWidth;
            var plotRight = card.Right - CardPadding;
            var plotTop = card.Top + HeaderHeight + TopPadding;
            var plotBottom = card.Top + HeaderHeight + ChartHeight;
            var plotHeight = plotBottom - plotTop;
            var spacing = (plotRight - plotLeft) / history.Count;

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

            for (var i = 0; i <= GridRows; i++)
            {
                var y = plotTop + plotHeight * i / GridRows;
                var value = max - (max - min) * i / GridRows;
                canvas.DrawLine(plotLeft, y, plotRight, y, gridPaint);
                canvas.DrawText(value.ToString("0.#"), plotLeft - 14, y + labelFont.Size * 0.35f,
                    SKTextAlign.Right, labelFont, mutedPaint);
            }

            var points = new SKPoint[history.Count];
            for (var i = 0; i < history.Count; i++)
            {
                var x = plotLeft + spacing * (i + 0.5f);
                var normalized = (float)((values[i] - min) / (max - min));
                points[i] = new SKPoint(x, plotBottom - normalized * plotHeight);
            }

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
                canvas.DrawCircle(p, 9, dotFillPaint);
                canvas.DrawCircle(p, 9, dotStrokePaint);
            }

            var step = Math.Max(1, (int)Math.Ceiling(MinLabelSpacing / spacing));
            var lastYear = -1;

            for (var i = 0; i < history.Count; i += step)
            {
                var h = history[i];
                var p = points[i];

                canvas.DrawText($"{h.Weight:0.##}×{h.Reps:0.##}", p.X, p.Y - 22, SKTextAlign.Center, labelFont, whitePaint);
                canvas.DrawText(h.Date.ToString("MMM d"), p.X, plotBottom + 42, SKTextAlign.Center, labelFont, whitePaint);

                if (h.Date.Year != lastYear)
                {
                    canvas.DrawText(h.Date.Year.ToString(), p.X, plotBottom + 76, SKTextAlign.Center, smallFont, mutedPaint);
                    lastYear = h.Date.Year;
                }
            }
        }
    }
}