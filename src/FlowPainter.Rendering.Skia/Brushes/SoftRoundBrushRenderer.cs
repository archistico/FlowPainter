using SkiaSharp;

namespace FlowPainter.Rendering.Skia.Brushes;

internal sealed class SoftRoundBrushRenderer : ISkiaBrushRenderer
{
    public void Draw(SKCanvas canvas, BrushRenderContext context, SKPaint paint)
    {
        using SKPath path = SkiaBrushPath.Create(context.Stroke, context.OutputSize);
        double softness = 1d - context.Settings.Hardness;
        if (softness <= 0.001d)
        {
            SkiaBrushPaint.Configure(
                paint,
                context.Color,
                context.WidthPixels,
                SKStrokeCap.Round,
                SKStrokeJoin.Round);
            canvas.DrawPath(path, paint);
            return;
        }

        DrawLayer(canvas, context, paint, path, 1d + (1.8d * softness), 0.12d + (0.08d * softness));
        DrawLayer(canvas, context, paint, path, 1d + (0.8d * softness), 0.24d + (0.10d * context.Settings.Hardness));
        DrawLayer(canvas, context, paint, path, 1d, 0.72d + (0.28d * context.Settings.Hardness));
    }

    private static void DrawLayer(
        SKCanvas canvas,
        BrushRenderContext context,
        SKPaint paint,
        SKPath path,
        double widthScale,
        double alphaScale)
    {
        SkiaBrushPaint.Configure(
            paint,
            SkiaBrushPaint.ScaleAlpha(context.Color, alphaScale),
            (float)(context.WidthPixels * widthScale),
            SKStrokeCap.Round,
            SKStrokeJoin.Round);
        canvas.DrawPath(path, paint);
    }
}
