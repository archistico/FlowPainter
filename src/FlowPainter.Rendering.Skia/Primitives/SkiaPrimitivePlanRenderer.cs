using System.Diagnostics.CodeAnalysis;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Primitives;
using FlowPainter.Imaging.Skia.Images;
using SkiaSharp;

namespace FlowPainter.Rendering.Skia.Primitives;

public sealed class SkiaPrimitivePlanRenderer
{
    private const int ProgressBatchSize = 64;

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "The renderer is an application service with instance semantics so it can later be decorated or replaced without changing call sites.")]
    public Task<SkiaImage> RenderAsync(
        PrimitivePlan plan,
        ImageSize outputSize,
        IProgress<PrimitiveRenderProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return Task.Run(
            () => Render(plan, outputSize, progress, cancellationToken),
            cancellationToken);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The SKBitmap ownership is transferred to the returned SkiaImage; every failure path disposes the wrapper.")]
    private static SkiaImage Render(
        PrimitivePlan plan,
        ImageSize outputSize,
        IProgress<PrimitiveRenderProgress>? progress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new PrimitiveRenderProgress(
            PrimitiveRenderStage.Preparing,
            0,
            plan.Primitives.Count,
            0d));

        SKImageInfo imageInfo = new(
            outputSize.Width,
            outputSize.Height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(imageInfo)
            ?? throw new InvalidOperationException("SkiaSharp could not create the primitive render surface.");
        SKCanvas canvas = surface.Canvas;
        canvas.Clear(ToSkiaColor(plan.BackgroundColor));
        progress?.Report(new PrimitiveRenderProgress(
            PrimitiveRenderStage.DrawingBackground,
            0,
            plan.Primitives.Count,
            0.02d));

        using SKPaint paint = new()
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        for (int index = 0; index < plan.Primitives.Count; index++)
        {
            if (index % ProgressBatchSize == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new PrimitiveRenderProgress(
                    PrimitiveRenderStage.DrawingPrimitives,
                    index,
                    plan.Primitives.Count,
                    CalculateFraction(index, plan.Primitives.Count)));
            }

            DrawPrimitive(canvas, paint, plan.Primitives[index], outputSize);
        }

        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new PrimitiveRenderProgress(
            PrimitiveRenderStage.Finalizing,
            plan.Primitives.Count,
            plan.Primitives.Count,
            0.98d));

        using SKImage snapshot = surface.Snapshot();
        SKBitmap bitmap = SKBitmap.FromImage(snapshot)
            ?? throw new InvalidOperationException("SkiaSharp could not copy the rendered primitive pixels.");
        SkiaImage result = new(bitmap);
        bool completed = false;

        try
        {
            progress?.Report(new PrimitiveRenderProgress(
                PrimitiveRenderStage.Completed,
                plan.Primitives.Count,
                plan.Primitives.Count,
                1d));
            completed = true;
            return result;
        }
        finally
        {
            if (!completed)
            {
                result.Dispose();
            }
        }
    }

    private static void DrawPrimitive(
        SKCanvas canvas,
        SKPaint paint,
        GeometricPrimitive primitive,
        ImageSize outputSize)
    {
        float centerX = (float)(primitive.Center.X * outputSize.Width);
        float centerY = (float)(primitive.Center.Y * outputSize.Height);
        float width = Math.Max(0.5f, (float)(primitive.Width * outputSize.Width));
        float height = Math.Max(0.5f, (float)(primitive.Height * outputSize.Height));
        paint.Color = ToSkiaColor(primitive.Color);

        if (primitive.Kind == PrimitiveKind.Triangle)
        {
            DrawTriangle(canvas, paint, centerX, centerY, width, height, primitive.RotationRadians);
            return;
        }

        canvas.Save();
        try
        {
            if (primitive.RotationRadians != 0d)
            {
                canvas.RotateDegrees(
                    (float)(primitive.RotationRadians * 180d / Math.PI),
                    centerX,
                    centerY);
            }

            SKRect bounds = new(
                centerX - (width * 0.5f),
                centerY - (height * 0.5f),
                centerX + (width * 0.5f),
                centerY + (height * 0.5f));
            switch (primitive.Kind)
            {
                case PrimitiveKind.Rectangle:
                case PrimitiveKind.RotatedRectangle:
                    canvas.DrawRect(bounds, paint);
                    break;
                case PrimitiveKind.Circle:
                    canvas.DrawCircle(centerX, centerY, Math.Min(width, height) * 0.5f, paint);
                    break;
                case PrimitiveKind.Ellipse:
                    canvas.DrawOval(bounds, paint);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(primitive),
                        primitive.Kind,
                        "Unknown primitive kind.");
            }
        }
        finally
        {
            canvas.Restore();
        }
    }

    private static void DrawTriangle(
        SKCanvas canvas,
        SKPaint paint,
        float centerX,
        float centerY,
        float width,
        float height,
        double rotationRadians)
    {
        double cosine = Math.Cos(rotationRadians);
        double sine = Math.Sin(rotationRadians);
        (float X, float Y) top = Rotate(0f, -height * 0.5f, centerX, centerY, cosine, sine);
        (float X, float Y) left = Rotate(-width * 0.5f, height * 0.5f, centerX, centerY, cosine, sine);
        (float X, float Y) right = Rotate(width * 0.5f, height * 0.5f, centerX, centerY, cosine, sine);
        using SKPathBuilder builder = new();
        builder.MoveTo(top.X, top.Y);
        builder.LineTo(left.X, left.Y);
        builder.LineTo(right.X, right.Y);
        builder.Close();
        using SKPath path = builder.Detach();
        canvas.DrawPath(path, paint);
    }

    private static (float X, float Y) Rotate(
        float x,
        float y,
        float centerX,
        float centerY,
        double cosine,
        double sine)
    {
        return (
            centerX + (float)((cosine * x) - (sine * y)),
            centerY + (float)((sine * x) + (cosine * y)));
    }

    private static SKColor ToSkiaColor(FlowPainter.Domain.Color.Rgba32 color)
    {
        return new SKColor(color.Red, color.Green, color.Blue, color.Alpha);
    }

    private static double CalculateFraction(int completed, int total)
    {
        return total == 0 ? 0.95d : 0.05d + (0.9d * completed / total);
    }
}
