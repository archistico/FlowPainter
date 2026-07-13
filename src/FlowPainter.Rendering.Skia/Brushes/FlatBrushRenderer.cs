using SkiaSharp;

namespace FlowPainter.Rendering.Skia.Brushes;

internal sealed class FlatBrushRenderer : ISkiaBrushRenderer
{
    public void Draw(SKCanvas canvas, BrushRenderContext context, SKPaint paint)
    {
        using SKPath path = SkiaBrushPath.Create(context.Stroke, context.OutputSize);
        SkiaBrushPaint.Configure(
            paint,
            context.Color,
            context.WidthPixels,
            SKStrokeCap.Square,
            SKStrokeJoin.Bevel);
        canvas.DrawPath(path, paint);
    }
}
