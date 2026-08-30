using SkiaSharp;
using FitISO.Maui.Models;

namespace FitISO.Maui.Services
{
    public static class WorkoutImageBuilder
    {
        const float Width = 1080f;
        const float OuterMargin = 32f;          
        const float CardPaddingX = 48f;
        const float CardCornerRadius = 72f;
        const float CardWidth = Width - OuterMargin * 2;
        const float SetKgColumnWidth = 90f;

        public static void Build(Workout workout, Stream output)
        {
            var totalHeight = RenderContent(null, workout);

            using var bitmap = new SKBitmap((int)Width, (int)Math.Ceiling(totalHeight));
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.Transparent); 

            RenderContent(canvas, workout);

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
        static float RenderContent(SKCanvas? canvas, Workout workout)
        {
            using var boldTypeface = SKTypeface.FromFamilyName(null, SKFontStyle.Bold);
            using var titleFont = new SKFont(boldTypeface, 44);
            using var pillFont = new SKFont(SKTypeface.Default, 32);
            using var exerciseFont = new SKFont(SKTypeface.Default, 34);
            using var rowFont = new SKFont(SKTypeface.Default, 34);

            var cardPaint = new SKPaint { Color = ResolveColor("Gray900", new SKColor(0x21, 0x21, 0x21)), IsAntialias = true };
            var datePillPaint = new SKPaint { Color = ResolveColor("Gray700", new SKColor(0x36, 0x36, 0x36)), IsAntialias = true };
            var timePillPaint = new SKPaint { Color = ResolveColor("Gray600", new SKColor(0x40, 0x40, 0x40)), IsAntialias = true };
            var exerciseHeaderPaint = new SKPaint { Color = ResolveColor("Gray700", new SKColor(0x36, 0x36, 0x36)), IsAntialias = true };
            var whitePaint = new SKPaint { Color = ResolveColor("White", SKColors.White), IsAntialias = true };
            var gray400Paint = new SKPaint { Color = ResolveColor("Gray400", new SKColor(0x91, 0x91, 0x91)), IsAntialias = true };

            float measuredInnerHeight = MeasureOrDrawInner(null, workout, titleFont, pillFont, exerciseFont, rowFont,
                datePillPaint, timePillPaint, exerciseHeaderPaint, whitePaint, gray400Paint);

            float cardHeight = measuredInnerHeight;

            canvas?.DrawRoundRect(
                new SKRoundRect(new SKRect(OuterMargin, OuterMargin, Width - OuterMargin, OuterMargin + cardHeight), CardCornerRadius),
                cardPaint);

            MeasureOrDrawInner(canvas, workout, titleFont, pillFont, exerciseFont, rowFont,
                datePillPaint, timePillPaint, exerciseHeaderPaint, whitePaint, gray400Paint);

            return OuterMargin + cardHeight + OuterMargin;
        }

        static DateTime ToLocal(DateTime? dt)
        {
            var value = dt.GetValueOrDefault();
            if (value.Kind != DateTimeKind.Utc)
                value = DateTime.SpecifyKind(value, DateTimeKind.Utc);

            return value.ToLocalTime();
        }

        static float MeasureOrDrawInner(SKCanvas? canvas, Workout workout,
           SKFont titleFont, SKFont pillFont, SKFont exerciseFont, SKFont rowFont,
           SKPaint datePillPaint, SKPaint timePillPaint, SKPaint exerciseHeaderPaint, SKPaint whitePaint, SKPaint gray400Paint)
        {
            float cardLeft = OuterMargin;
            float cardCenterX = Width / 2;
            float y = OuterMargin + 56;

            canvas?.DrawText(workout.Name, cardCenterX, y, SKTextAlign.Center, titleFont, whitePaint);
            y += 52;

            var dateText = ToLocal(workout.StartTime).ToString("dd/MM/yyyy");
            var endTime = workout.EndTime ?? workout.StartTime;
            var startText = ToLocal(workout.StartTime).ToString("HH:mm");
            var endText = workout.EndTime is null ? "--:--" : ToLocal(endTime).ToString("HH:mm");
            const float arrowWidth = 22f;
            const float arrowHeight = 16f;

            float pillHeight = 60;
            float pillPaddingX = 24;
            float pillGap = 24;

            float dateTextWidth = pillFont.MeasureText(dateText);
            float datePillWidth = dateTextWidth + pillPaddingX * 2;

            float timeGap = 14;
            float startWidth = pillFont.MeasureText(startText);
            float endWidth = pillFont.MeasureText(endText);
            float timeInnerWidth = startWidth + timeGap + arrowWidth + timeGap + endWidth;
            float timePillWidth = timeInnerWidth + pillPaddingX * 2;

            float pillsTotalWidth = datePillWidth + pillGap + timePillWidth;
            float pillsLeft = cardCenterX - pillsTotalWidth / 2;

            var dateRect = new SKRect(pillsLeft, y, pillsLeft + datePillWidth, y + pillHeight);
            canvas?.DrawRoundRect(new SKRoundRect(dateRect, pillHeight / 2), datePillPaint);
            canvas?.DrawText(dateText, dateRect.MidX, y + pillHeight / 2 + 11, SKTextAlign.Center, pillFont, whitePaint);

            var timeRect = new SKRect(dateRect.Right + pillGap, y, dateRect.Right + pillGap + timePillWidth, y + pillHeight);
            canvas?.DrawRoundRect(new SKRoundRect(timeRect, pillHeight / 2), timePillPaint);

            float timeTextY = y + pillHeight / 2 + 11;
            float pillCenterY = y + pillHeight / 2;
            float cursorX = timeRect.Left + pillPaddingX;
            canvas?.DrawText(startText, cursorX, timeTextY, SKTextAlign.Left, pillFont, whitePaint);
            cursorX += startWidth + timeGap;
            DrawArrow(canvas, cursorX, pillCenterY, arrowWidth, arrowHeight, whitePaint);
            cursorX += arrowWidth + timeGap;
            canvas?.DrawText(endText, cursorX, timeTextY, SKTextAlign.Left, pillFont, whitePaint);

            y += pillHeight + 40;

            foreach (var we in workout.WorkoutExercises)
            {
                float headerHeight = 64;
                canvas?.DrawRect(new SKRect(cardLeft, y, cardLeft + CardWidth, y + headerHeight), exerciseHeaderPaint);
                canvas?.DrawText(we.Exercise.Name, cardCenterX, y + headerHeight / 2 + 12, SKTextAlign.Center, exerciseFont, whitePaint);
                y += headerHeight;

                float rowLeft = cardLeft + CardPaddingX;
                float rowRight = cardLeft + CardWidth - CardPaddingX;
                float weightColRight = rowLeft + (rowRight - rowLeft - SetKgColumnWidth) * 1.15f / 3.15f;
                float repsColCenter = weightColRight + SetKgColumnWidth + (rowRight - weightColRight - SetKgColumnWidth) / 2f;

                foreach (var set in we.Sets)
                {
                    float rowHeight = 60;
                    var weight = set.Weight.GetValueOrDefault();
                    var reps = set.Reps.GetValueOrDefault();
                    float textY = y + rowHeight / 2 + 12;

                    canvas?.DrawText($"{weight:0.##}", weightColRight, textY, SKTextAlign.Right, rowFont, whitePaint);
                    canvas?.DrawText("kg", weightColRight + 16, textY, SKTextAlign.Left, rowFont, gray400Paint);
                    canvas?.DrawText($"{reps:0.##}", repsColCenter, textY, SKTextAlign.Center, rowFont, whitePaint);

                    y += rowHeight;
                }
            }

            y += 24;

            return y - OuterMargin;
        }

        static void DrawArrow(SKCanvas? canvas, float left, float centerY, float width, float height, SKPaint paint)
        {
            if (canvas is null) return;

            using var path = new SKPath();
            path.MoveTo(left, centerY - height / 2);
            path.LineTo(left, centerY + height / 2);
            path.LineTo(left + width, centerY);
            path.Close();

            canvas.DrawPath(path, paint);
        }
    }
}