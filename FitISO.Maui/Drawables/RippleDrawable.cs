namespace FitISO.Maui.Drawables
{
    public sealed class RippleDrawable : IDrawable
    {
        const float PeakAlpha = 0.35f;

        public float Progress { get; set; }        
        public Color? RippleColor { get; set; }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (RippleColor is null || Progress <= 0f || Progress >= 1f)
                return;

            float cx = dirtyRect.Width / 2f;
            float cy = dirtyRect.Height / 2f;
            float maxRadius = MathF.Sqrt(cx * cx + cy * cy); 

            canvas.SaveState();
            canvas.FillColor = RippleColor.WithAlpha(PeakAlpha * (1f - Progress));
            canvas.FillCircle(cx, cy, maxRadius * Progress);
            canvas.RestoreState();
        }
    }
}