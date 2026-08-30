using SkiaSharp;
using FitISO.Maui.Models;

namespace FitISO.Maui.Services
{
    public static class WorkoutPdfBuilder
    {
        const float PageWidth = 595f;
        const float PageHeight = 842f;
        const float Margin = 40f;

        static DateTime ToLocal(DateTime? dt)
        {
            var value = dt.GetValueOrDefault();
            if (value.Kind != DateTimeKind.Utc)
                value = DateTime.SpecifyKind(value, DateTimeKind.Utc);

            return value.ToLocalTime();
        }

        public static void Build(Workout workout, Stream output)
        {
            using var document = SKDocument.CreatePdf(output);

            using var boldTypeface = SKTypeface.FromFamilyName(null, SKFontStyle.Bold);
            using var titleFont = new SKFont(boldTypeface, 22);
            using var subFont = new SKFont(SKTypeface.Default, 12);
            using var headerFont = new SKFont(boldTypeface, 14);
            using var rowFont = new SKFont(SKTypeface.Default, 12);

            var blackPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            var grayPaint = new SKPaint { Color = new SKColor(90, 90, 90), IsAntialias = true };
            var linePaint = new SKPaint { Color = new SKColor(210, 210, 210), StrokeWidth = 1 };

            SKCanvas canvas = null;
            float y = 0;

            void NewPage()
            {
                canvas = document.BeginPage(PageWidth, PageHeight);
                y = Margin;
            }

            void EnsureSpace(float needed)
            {
                if (y + needed > PageHeight - Margin)
                {
                    document.EndPage();
                    NewPage();
                }
            }

            NewPage();

            canvas.DrawText(workout.Name, Margin, y + 20, SKTextAlign.Left, titleFont, blackPaint);
            y += 44;

            var startLocal = ToLocal(workout.StartTime);
            var endTime = workout.EndTime ?? workout.StartTime;
            var durationMinutes = (int)(endTime - workout.StartTime).Value.TotalMinutes;
            var endTimeText = workout.EndTime is null ? "--:--" : ToLocal(endTime).ToString("HH:mm");

            canvas.DrawText($"{startLocal:dddd, MMMM d, yyyy}", Margin, y, SKTextAlign.Left, subFont, grayPaint);
            y += 18;
            canvas.DrawText($"{startLocal:HH:mm} - {endTimeText}  ({durationMinutes} min)", Margin, y, SKTextAlign.Left, subFont, grayPaint);
            y += 24;

            canvas.DrawLine(Margin, y, PageWidth - Margin, y, linePaint);
            y += 20;

            foreach (var we in workout.WorkoutExercises)
            {
                EnsureSpace(30 + 18 * Math.Max(we.Sets.Count, 1));

                canvas.DrawText(we.Exercise.Name, Margin, y, SKTextAlign.Left, headerFont, blackPaint);
                y += 18;

                canvas.DrawText("Set", Margin, y, SKTextAlign.Left, subFont, grayPaint);
                canvas.DrawText("Weight", Margin + 60, y, SKTextAlign.Left, subFont, grayPaint);
                canvas.DrawText("Reps", Margin + 160, y, SKTextAlign.Left, subFont, grayPaint);
                y += 14;

                int setNumber = 1;
                foreach (var set in we.Sets)
                {
                    EnsureSpace(18);
                    canvas.DrawText(setNumber.ToString(), Margin, y, SKTextAlign.Left, rowFont, blackPaint);
                    canvas.DrawText($"{set.Weight:0.##} kg", Margin + 60, y, SKTextAlign.Left, rowFont, blackPaint);
                    canvas.DrawText($"{set.Reps:0.##}", Margin + 160, y, SKTextAlign.Left, rowFont, blackPaint);
                    y += 18;
                    setNumber++;
                }

                y += 12;
            }

            document.EndPage();
            document.Close();
        }
    }
}