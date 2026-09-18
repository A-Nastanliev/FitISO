using SkiaSharp;

namespace FitISO.Maui.Rendering
{
    static class SkiaDrawer
    {
        public static SKBitmap CreateTransparentBitmap(int width, int height, Action<SKCanvas> draw)
        {
            var bitmap = new SKBitmap(Math.Max(width, 1), Math.Max(height, 1));
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.Transparent);

            draw(canvas);

            return bitmap;
        }
    }
}