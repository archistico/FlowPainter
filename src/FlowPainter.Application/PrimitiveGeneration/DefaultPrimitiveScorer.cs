using FlowPainter.Domain.Color;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Primitives;

namespace FlowPainter.Application.PrimitiveGeneration;

public sealed class DefaultPrimitiveScorer : IPrimitiveScorer
{
    private readonly IPrimitiveMaskRasterizer _rasterizer;

    public DefaultPrimitiveScorer(IPrimitiveMaskRasterizer rasterizer)
    {
        ArgumentNullException.ThrowIfNull(rasterizer);
        _rasterizer = rasterizer;
    }

    public PrimitiveScore Score(
        ImageSize size,
        ReadOnlyMemory<Rgba32> sourcePixels,
        ReadOnlyMemory<Rgba32> currentPixels,
        DetailMap? detailMap,
        GeometricPrimitive candidate,
        PrimitiveGenerationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(settings);
        ValidateBuffer(size, sourcePixels, nameof(sourcePixels));
        ValidateBuffer(size, currentPixels, nameof(currentPixels));
        if (detailMap is not null && detailMap.Size != size)
        {
            throw new ArgumentException("The detail map must match the scoring image size.", nameof(detailMap));
        }

        PrimitiveRasterMask mask = _rasterizer.Rasterize(candidate, size);
        if (mask.PixelIndices.Count == 0)
        {
            return new PrimitiveScore(candidate, mask, 0d);
        }

        ReadOnlySpan<Rgba32> source = sourcePixels.Span;
        ReadOnlySpan<Rgba32> current = currentPixels.Span;
        Rgba32 optimalColor = EstimateOptimalColor(
            source,
            current,
            mask.PixelIndices,
            settings.Opacity);
        GeometricPrimitive colored = candidate.WithColor(optimalColor);
        double improvement = CalculateImprovement(
            size,
            source,
            current,
            detailMap,
            mask.PixelIndices,
            optimalColor,
            settings);
        return new PrimitiveScore(colored, mask, improvement);
    }

    private static Rgba32 EstimateOptimalColor(
        ReadOnlySpan<Rgba32> source,
        ReadOnlySpan<Rgba32> current,
        IReadOnlyList<int> indices,
        double opacity)
    {
        double inverseOpacity = 1d / opacity;
        double retained = 1d - opacity;
        double red = 0d;
        double green = 0d;
        double blue = 0d;

        foreach (int index in indices)
        {
            Rgba32 target = source[index];
            Rgba32 existing = current[index];
            red += (target.Red - (existing.Red * retained)) * inverseOpacity;
            green += (target.Green - (existing.Green * retained)) * inverseOpacity;
            blue += (target.Blue - (existing.Blue * retained)) * inverseOpacity;
        }

        double divisor = indices.Count;
        byte alpha = checked((byte)Math.Round(byte.MaxValue * opacity, MidpointRounding.AwayFromZero));
        return new Rgba32(
            ClampByte(red / divisor),
            ClampByte(green / divisor),
            ClampByte(blue / divisor),
            alpha);
    }

    private static double CalculateImprovement(
        ImageSize size,
        ReadOnlySpan<Rgba32> source,
        ReadOnlySpan<Rgba32> current,
        DetailMap? detailMap,
        IReadOnlyList<int> indices,
        Rgba32 color,
        PrimitiveGenerationSettings settings)
    {
        double improvement = 0d;
        foreach (int index in indices)
        {
            Rgba32 target = source[index];
            Rgba32 existing = current[index];
            Rgba32 blended = Blend(existing, color);
            double weight = 1d;
            if (detailMap is not null)
            {
                int x = index % size.Width;
                int y = index / size.Width;
                weight += settings.DetailErrorWeight * detailMap[x, y];
            }

            improvement += weight
                * (SquaredError(existing, target) - SquaredError(blended, target));
        }

        return improvement;
    }

    internal static Rgba32 Blend(Rgba32 background, Rgba32 foreground)
    {
        double alpha = foreground.Alpha / 255d;
        double inverse = 1d - alpha;
        return new Rgba32(
            ClampByte((foreground.Red * alpha) + (background.Red * inverse)),
            ClampByte((foreground.Green * alpha) + (background.Green * inverse)),
            ClampByte((foreground.Blue * alpha) + (background.Blue * inverse)),
            byte.MaxValue);
    }

    private static double SquaredError(Rgba32 left, Rgba32 right)
    {
        double red = left.Red - right.Red;
        double green = left.Green - right.Green;
        double blue = left.Blue - right.Blue;
        return (red * red) + (green * green) + (blue * blue);
    }

    private static byte ClampByte(double value)
    {
        return checked((byte)Math.Round(Math.Clamp(value, 0d, byte.MaxValue), MidpointRounding.AwayFromZero));
    }

    private static void ValidateBuffer(
        ImageSize size,
        ReadOnlyMemory<Rgba32> pixels,
        string parameterName)
    {
        if (pixels.Length != size.PixelCount)
        {
            throw new ArgumentException(
                $"Expected {size.PixelCount} pixels but received {pixels.Length}.",
                parameterName);
        }
    }
}
