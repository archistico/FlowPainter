using SkiaSharp;

namespace FlowPainter.Rendering.Skia.Brushes;

internal sealed class BristleBrushRenderer : ISkiaBrushRenderer
{
    public void Draw(SKCanvas canvas, BrushRenderContext context, SKPaint paint)
    {
        SKPoint[] points = SkiaBrushPath.CreatePixelPoints(context.Stroke, context.OutputSize);
        int count = context.Settings.BristleCount;
        float spread = context.WidthPixels * (float)context.Settings.BristleSpread;
        float baseThickness = Math.Max(0.35f, context.WidthPixels / Math.Max(2f, count * 0.72f));
        double irregularity = 1d - context.Settings.Hardness;

        for (int index = 0; index < count; index++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            double position = count == 1
                ? 0d
                : ((double)index / (count - 1)) - 0.5d;
            double randomOffsetSample = context.Random.NextDouble();
            double randomOffset = Math.Abs(position) < double.Epsilon
                ? 0d
                : (randomOffsetSample - 0.5d)
                    * irregularity
                    * context.WidthPixels
                    * 0.35d;
            float offset = (float)((position * spread) + randomOffset);
            float thickness = baseThickness * (float)(0.75d + (context.Random.NextDouble() * 0.5d));
            double alphaScale = 0.45d + (context.Random.NextDouble() * 0.5d);

            using SKPath path = SkiaBrushPath.CreateOffset(points, offset);
            SkiaBrushPaint.Configure(
                paint,
                SkiaBrushPaint.ScaleAlpha(context.Color, alphaScale),
                thickness,
                SKStrokeCap.Round,
                SKStrokeJoin.Round);
            canvas.DrawPath(path, paint);
        }
    }
}
