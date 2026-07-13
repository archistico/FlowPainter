using SkiaSharp;

namespace FlowPainter.Rendering.Skia.Brushes;

internal interface ISkiaBrushRenderer
{
    void Draw(SKCanvas canvas, BrushRenderContext context, SKPaint paint);
}
