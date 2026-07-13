using System.Diagnostics.CodeAnalysis;
using FlowPainter.Domain.Detail;
using FlowPainter.Imaging.Skia.Images;
using SkiaSharp;

namespace FlowPainter.Rendering.Skia.Detail;

public sealed class DetailMapOverlayRenderer
{
    public const double DefaultOpacity = 0.55d;

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "The renderer is an application service with instance semantics so future overlay styles and decorators can replace it without changing call sites.")]
    public Task<SkiaImage> RenderAsync(
        SkiaImage source,
        DetailMap detailMap,
        double opacity = DefaultOpacity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(detailMap);

        if (source.Size != detailMap.Size)
        {
            throw new ArgumentException(
                "The source image and detail map must have identical dimensions.",
                nameof(detailMap));
        }

        if (!double.IsFinite(opacity) || opacity < 0d || opacity > 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(opacity),
                opacity,
                "Overlay opacity must be finite and between 0 and 1.");
        }

        return Task.Run(
            () => Render(source, detailMap, opacity, cancellationToken),
            cancellationToken);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The output SKBitmap ownership is transferred to the returned SkiaImage; failure paths dispose it explicitly.")]
    private static SkiaImage Render(
        SkiaImage source,
        DetailMap detailMap,
        double opacity,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SKBitmap sourceBitmap = source.GetBitmap();
        SKImageInfo imageInfo = new(
            source.Size.Width,
            source.Size.Height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul);
        SKBitmap output = new(imageInfo);
        bool adopted = false;

        try
        {
            for (int y = 0; y < source.Size.Height; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                for (int x = 0; x < source.Size.Width; x++)
                {
                    SKColor original = sourceBitmap.GetPixel(x, y);
                    double detail = detailMap[x, y];
                    SKColor heat = CreateHeatColor(detail);
                    output.SetPixel(x, y, Blend(original, heat, opacity));
                }
            }

            SkiaImage result = new(output, source.SourceName);
            adopted = true;
            return result;
        }
        finally
        {
            if (!adopted)
            {
                output.Dispose();
            }
        }
    }

    private static SKColor CreateHeatColor(double detail)
    {
        byte red = checked((byte)Math.Round(255d * detail, MidpointRounding.AwayFromZero));
        byte blue = checked((byte)Math.Round(255d * (1d - detail), MidpointRounding.AwayFromZero));
        double middle = 1d - Math.Abs((2d * detail) - 1d);
        byte green = checked((byte)Math.Round(220d * middle, MidpointRounding.AwayFromZero));
        return new SKColor(red, green, blue, byte.MaxValue);
    }

    private static SKColor Blend(SKColor background, SKColor foreground, double opacity)
    {
        double inverse = 1d - opacity;
        byte red = BlendChannel(background.Red, foreground.Red, inverse, opacity);
        byte green = BlendChannel(background.Green, foreground.Green, inverse, opacity);
        byte blue = BlendChannel(background.Blue, foreground.Blue, inverse, opacity);
        return new SKColor(red, green, blue, background.Alpha);
    }

    private static byte BlendChannel(
        byte background,
        byte foreground,
        double backgroundWeight,
        double foregroundWeight)
    {
        return checked((byte)Math.Round(
            (background * backgroundWeight) + (foreground * foregroundWeight),
            MidpointRounding.AwayFromZero));
    }
}
