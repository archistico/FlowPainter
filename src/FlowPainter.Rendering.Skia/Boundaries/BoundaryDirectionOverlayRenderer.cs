using System.Diagnostics.CodeAnalysis;
using FlowPainter.Domain.Boundaries;
using FlowPainter.Domain.Detail;
using FlowPainter.Imaging.Skia.Images;
using SkiaSharp;

namespace FlowPainter.Rendering.Skia.Boundaries;

public sealed class BoundaryDirectionOverlayRenderer
{
    private static readonly SKSamplingOptions SourceSampling = new(SKFilterMode.Linear, SKMipmapMode.Linear);

    public const int DefaultGridSpacing = 12;
    public const double DefaultMinimumImportance = 0.3d;
    public const double DefaultOpacity = 0.78d;

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "The renderer is an application service with instance semantics so styles and decorators can replace it without changing call sites.")]
    public Task<SkiaImage> RenderAsync(
        SkiaImage source,
        BoundaryDirectionField directionField,
        DetailMap importanceMap,
        int gridSpacing = DefaultGridSpacing,
        double minimumImportance = DefaultMinimumImportance,
        double opacity = DefaultOpacity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(directionField);
        ArgumentNullException.ThrowIfNull(importanceMap);

        if (source.Size != directionField.Size || source.Size != importanceMap.Size)
        {
            throw new ArgumentException(
                "The source, direction field and importance map must have identical dimensions.",
                nameof(directionField));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(gridSpacing, 2);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(gridSpacing, 128);
        ValidateUnitInterval(minimumImportance, nameof(minimumImportance));
        ValidateUnitInterval(opacity, nameof(opacity));

        return Task.Run(
            () => Render(
                source,
                directionField,
                importanceMap,
                gridSpacing,
                minimumImportance,
                opacity,
                cancellationToken),
            cancellationToken);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The output bitmap ownership is transferred to the returned SkiaImage and disposed on every failure path.")]
    private static SkiaImage Render(
        SkiaImage source,
        BoundaryDirectionField directionField,
        DetailMap importanceMap,
        int gridSpacing,
        double minimumImportance,
        double opacity,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SKImageInfo imageInfo = new(
            source.Size.Width,
            source.Size.Height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(imageInfo)
            ?? throw new InvalidOperationException("SkiaSharp could not create the boundary-direction overlay surface.");
        SKCanvas canvas = surface.Canvas;
        using (SKImage sourceImage = source.CreateSnapshot())
        {
            canvas.DrawImage(
                sourceImage,
                new SKRect(0f, 0f, source.Size.Width, source.Size.Height),
                SourceSampling);
        }

        using SKPaint shadowPaint = new()
        {
            IsAntialias = true,
            Color = new SKColor(0, 0, 0, checked((byte)Math.Round(180d * opacity))),
            StrokeWidth = 3f,
            StrokeCap = SKStrokeCap.Round,
            Style = SKPaintStyle.Stroke
        };
        using SKPaint directionPaint = new()
        {
            IsAntialias = true,
            Color = new SKColor(60, 235, 255, checked((byte)Math.Round(255d * opacity))),
            StrokeWidth = 1.4f,
            StrokeCap = SKStrokeCap.Round,
            Style = SKPaintStyle.Stroke
        };

        double halfLength = gridSpacing * 0.38d;
        int offset = gridSpacing / 2;
        using SKPathBuilder pathBuilder = new();
        bool hasDirections = false;
        for (int y = offset; y < source.Size.Height; y += gridSpacing)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (int x = offset; x < source.Size.Width; x += gridSpacing)
            {
                double importance = importanceMap[x, y];
                if (importance < minimumImportance)
                {
                    continue;
                }

                BoundaryVector direction = directionField[x, y];
                if (!direction.IsDefined)
                {
                    continue;
                }

                float startX = (float)(x - (direction.X * halfLength));
                float startY = (float)(y - (direction.Y * halfLength));
                float endX = (float)(x + (direction.X * halfLength));
                float endY = (float)(y + (direction.Y * halfLength));
                pathBuilder.MoveTo(startX, startY);
                pathBuilder.LineTo(endX, endY);
                hasDirections = true;
            }
        }

        if (hasDirections)
        {
            using SKPath path = pathBuilder.Detach();
            canvas.DrawPath(path, shadowPaint);
            canvas.DrawPath(path, directionPaint);
        }

        using SKImage snapshot = surface.Snapshot();
        SKBitmap bitmap = SKBitmap.FromImage(snapshot)
            ?? throw new InvalidOperationException("SkiaSharp could not copy the boundary-direction overlay pixels.");
        SkiaImage result = new(bitmap, source.SourceName);
        bool completed = false;
        try
        {
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

    private static void ValidateUnitInterval(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d || value > 1d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The value must be finite and between 0 and 1.");
        }
    }
}
