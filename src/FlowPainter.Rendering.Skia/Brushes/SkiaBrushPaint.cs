using SkiaSharp;

namespace FlowPainter.Rendering.Skia.Brushes;

internal static class SkiaBrushPaint
{
    public static void Configure(
        SKPaint paint,
        SKColor color,
        float width,
        SKStrokeCap cap,
        SKStrokeJoin join)
    {
        paint.IsAntialias = true;
        paint.Style = SKPaintStyle.Stroke;
        paint.Color = color;
        paint.StrokeWidth = Math.Max(0.35f, width);
        paint.StrokeCap = cap;
        paint.StrokeJoin = join;
    }

    public static SKColor ScaleAlpha(SKColor color, double scale)
    {
        double alpha = Math.Clamp(color.Alpha * scale, 0d, byte.MaxValue);
        return new SKColor(color.Red, color.Green, color.Blue, (byte)Math.Round(alpha));
    }
}
