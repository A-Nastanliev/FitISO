using SkiaSharp;

namespace FitISO.Maui.Rendering
{
    public static class HeatmapChartDrawer
    {
        const float CellGapRatio = 0.18f;

        public static SKBitmap Draw(HashSet<int> workoutDays, int today, int daysInMonth, DayOfWeek firstDayOfWeek,
            SKColor workoutColor, SKColor restColor, SKColor futureColor, int width, int height)
        {
            return SkiaDrawer.CreateTransparentBitmap(width, height, canvas =>
                Draw(canvas, workoutDays, today, daysInMonth, firstDayOfWeek,
                    workoutColor, restColor, futureColor, width, height));
        }

        public static void Draw(SKCanvas canvas, HashSet<int> workoutDays, int today, int daysInMonth, DayOfWeek firstDayOfWeek,
            SKColor workoutColor, SKColor restColor, SKColor futureColor, int width, int height)
        {
            if (daysInMonth <= 0)
                return;

            var leadingBlanks = ((int)firstDayOfWeek + 6) % 7;
            var totalCells = leadingBlanks + daysInMonth;

            const int Columns = 7;
            var rows = (int)Math.Ceiling(totalCells / (double)Columns);

            var cellWidth = (float)width / Columns;
            var cellHeight = (float)height / rows;
            var cellSize = Math.Min(cellWidth, cellHeight);

            var gridWidth = cellSize * Columns;
            var gridHeight = cellSize * rows;
            var offsetX = (width - gridWidth) / 2f;
            var offsetY = (height - gridHeight) / 2f;

            var gap = cellSize * CellGapRatio;
            var squareSize = cellSize - gap;
            var cornerRadius = squareSize * 0.22f;

            using var paint = new SKPaint { IsAntialias = true };

            for (var day = 1; day <= daysInMonth; day++)
            {
                var cellIndex = leadingBlanks + day - 1;
                var row = cellIndex / Columns;
                var col = cellIndex % Columns;

                var left = offsetX + col * cellSize + gap / 2f;
                var top = offsetY + row * cellSize + gap / 2f;
                var rect = new SKRect(left, top, left + squareSize, top + squareSize);

                paint.Color = day > today
                    ? futureColor
                    : (workoutDays.Contains(day) ? workoutColor : restColor);

                canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, paint);
            }
        }
    }
}