using SkiaSharp;

namespace FlowPainter.Rendering.Skia.Brushes;

internal sealed class SolidRoundBrushRenderer : ISkiaBrushRenderer
{
    public void Draw(SKCanvas canvas, BrushRenderContext context, SKPaint paint)
    {
        using SKPath path = SkiaBrushPath.Create(context.Stroke, context.OutputSize);
        SkiaBrushPaint.Configure(
            paint,
            context.Color,
            context.WidthPixels,
            SKStrokeCap.Round,
            SKStrokeJoin.Round);
        canvas.DrawPath(path, paint);
    }
}
