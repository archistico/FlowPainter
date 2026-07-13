using System.Diagnostics.CodeAnalysis;
using FlowPainter.Domain.Brushes;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Randomness;
using FlowPainter.Domain.Strokes;
using FlowPainter.Imaging.Skia.Images;
using FlowPainter.Rendering.Skia.Brushes;
using SkiaSharp;

namespace FlowPainter.Rendering.Skia.Strokes;

public sealed class SkiaStrokePlanRenderer
{
    private const int ProgressBatchSize = 256;
    private static readonly SKSamplingOptions BackgroundSampling = new(SKFilterMode.Linear, SKMipmapMode.Linear);

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "The renderer is an application service with instance semantics so future brush renderers and decorators can replace it without changing call sites.")]
    public Task<SkiaImage> RenderAsync(
        StrokePlan plan,
        ImageSize outputSize,
        SkiaImage? sourceBackground = null,
        IProgress<StrokeRenderProgress>? progress = null,
        BrushSettings? brush = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ValidateBackground(plan, sourceBackground);

        BrushSettings effectiveBrush = brush ?? new BrushSettings();
        return Task.Run(
            () => Render(plan, outputSize, sourceBackground, effectiveBrush, progress, cancellationToken),
            cancellationToken);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The rendered SKBitmap ownership is transferred to the returned SkiaImage; every failure path disposes that wrapper.")]
    private static SkiaImage Render(
        StrokePlan plan,
        ImageSize outputSize,
        SkiaImage? sourceBackground,
        BrushSettings brush,
        IProgress<StrokeRenderProgress>? progress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new StrokeRenderProgress(
            StrokeRenderStage.Preparing,
            0,
            plan.Strokes.Count,
            0d));

        SKImageInfo imageInfo = new(
            outputSize.Width,
            outputSize.Height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(imageInfo)
            ?? throw new InvalidOperationException("SkiaSharp could not create the render surface.");

        SKCanvas canvas = surface.Canvas;
        DrawBackground(canvas, plan, outputSize, sourceBackground, progress);

        using SKPaint paint = new();
        ISkiaBrushRenderer brushRenderer = SkiaBrushRendererFactory.Get(brush.Kind);
        int maximumOutputDimension = Math.Max(outputSize.Width, outputSize.Height);
        for (int index = 0; index < plan.Strokes.Count; index++)
        {
            if (index % ProgressBatchSize == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new StrokeRenderProgress(
                    StrokeRenderStage.DrawingStrokes,
                    index,
                    plan.Strokes.Count,
                    CalculateFraction(index, plan.Strokes.Count)));
            }

            DrawStroke(
                canvas,
                plan.Strokes[index],
                plan.Seed,
                outputSize,
                maximumOutputDimension,
                brush,
                brushRenderer,
                paint,
                cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new StrokeRenderProgress(
            StrokeRenderStage.Finalizing,
            plan.Strokes.Count,
            plan.Strokes.Count,
            0.98d));

        using SKImage snapshot = surface.Snapshot();
        SKBitmap bitmap = SKBitmap.FromImage(snapshot)
            ?? throw new InvalidOperationException("SkiaSharp could not copy the rendered pixels.");
        SkiaImage result = new(bitmap, sourceBackground?.SourceName);
        bool completed = false;

        try
        {
            progress?.Report(new StrokeRenderProgress(
                StrokeRenderStage.Completed,
                plan.Strokes.Count,
                plan.Strokes.Count,
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

    private static void DrawBackground(
        SKCanvas canvas,
        StrokePlan plan,
        ImageSize outputSize,
        SkiaImage? sourceBackground,
        IProgress<StrokeRenderProgress>? progress)
    {
        progress?.Report(new StrokeRenderProgress(
            StrokeRenderStage.DrawingBackground,
            0,
            plan.Strokes.Count,
            0.02d));

        if (plan.BackgroundMode == StrokePlanBackgroundMode.Transparent)
        {
            canvas.Clear(SKColors.Transparent);
            return;
        }

        using SKImage sourceImage = sourceBackground!.CreateSnapshot();
        canvas.Clear(SKColors.Transparent);
        canvas.DrawImage(
            sourceImage,
            new SKRect(0f, 0f, outputSize.Width, outputSize.Height),
            BackgroundSampling);
    }

    private static void DrawStroke(
        SKCanvas canvas,
        FlowStroke stroke,
        ulong planSeed,
        ImageSize outputSize,
        int maximumOutputDimension,
        BrushSettings brush,
        ISkiaBrushRenderer brushRenderer,
        SKPaint paint,
        CancellationToken cancellationToken)
    {
        DeterministicRandom random = new(CreateStrokeSeed(planSeed, stroke.Index));
        double sizeScale = 1d + (((random.NextDouble() * 2d) - 1d) * brush.SizeJitter);
        double opacityScale = 1d + (((random.NextDouble() * 2d) - 1d) * brush.OpacityJitter);
        float width = Math.Max(
            0.5f,
            (float)(stroke.WidthRelativeToReference * maximumOutputDimension * sizeScale));
        SKColor color = SkiaBrushPaint.ScaleAlpha(
            new SKColor(
                stroke.Color.Red,
                stroke.Color.Green,
                stroke.Color.Blue,
                stroke.Color.Alpha),
            opacityScale);
        BrushRenderContext context = new(
            stroke,
            outputSize,
            brush,
            width,
            color,
            random,
            cancellationToken);
        brushRenderer.Draw(canvas, context, paint);
    }

    private static ulong CreateStrokeSeed(ulong planSeed, int strokeIndex)
    {
        unchecked
        {
            ulong value = planSeed ^ ((ulong)(uint)strokeIndex + 0x9E3779B97F4A7C15UL);
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }

    private static void ValidateBackground(StrokePlan plan, SkiaImage? sourceBackground)
    {
        if (plan.BackgroundMode == StrokePlanBackgroundMode.SourceImage && sourceBackground is null)
        {
            throw new ArgumentNullException(
                nameof(sourceBackground),
                "A source image is required when the plan uses the source-image background mode.");
        }

        if (sourceBackground is not null)
        {
            int planMaximumDimension = Math.Max(plan.SourceSize.Width, plan.SourceSize.Height);
            ImageSize expectedPlanSize = sourceBackground.Size.FitWithin(
                planMaximumDimension,
                planMaximumDimension);
            if (expectedPlanSize != plan.SourceSize)
            {
                throw new ArgumentException(
                    "The source background dimensions are not compatible with the image used to create the stroke plan.",
                    nameof(sourceBackground));
            }
        }
    }

    private static double CalculateFraction(int completed, int total)
    {
        return total == 0 ? 0.95d : 0.05d + (0.9d * completed / total);
    }
}
