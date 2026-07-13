using System.Diagnostics.CodeAnalysis;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Detail;

public sealed class ImageDetailAnalyzer : IDetailMapAnalyzer
{
    private const int ProgressRowBatch = 16;
    private const double MaximumRgbDistance = 441.6729559300637d;

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "The analyzer is an application service with instance semantics so future semantic analyzers and decorators can replace it without changing call sites.")]
    public Task<DetailMap> AnalyzeAsync(
        IRgbaPixelSource source,
        DetailAnalysisSettings settings,
        IProgress<DetailAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(settings);

        return Task.Run(
            () => Analyze(source, settings, progress, cancellationToken),
            cancellationToken);
    }

    private static DetailMap Analyze(
        IRgbaPixelSource source,
        DetailAnalysisSettings settings,
        IProgress<DetailAnalysisProgress>? progress,
        CancellationToken cancellationToken)
    {
        ImageSize size = source.Size;
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new DetailAnalysisProgress(
            DetailAnalysisStage.Preparing,
            0,
            size.Height,
            0d));

        Rgba32[] pixels = ReadPixels(source, cancellationToken);
        float[] values = new float[checked((int)size.PixelCount)];

        for (int y = 0; y < size.Height; y++)
        {
            if (y % ProgressRowBatch == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new DetailAnalysisProgress(
                    DetailAnalysisStage.AnalyzingStructure,
                    y,
                    size.Height,
                    0.02d + (0.68d * y / size.Height)));
            }

            for (int x = 0; x < size.Width; x++)
            {
                double edge = CalculateEdgeSignal(pixels, size, x, y);
                double contrast = CalculateContrastSignal(pixels, size, x, y);
                double structuralSignal = Math.Clamp(
                    (settings.EdgeWeight * edge)
                    + (settings.ContrastWeight * contrast),
                    0d,
                    1d);
                double detail = settings.BaseDetail
                    + ((1d - settings.BaseDetail) * structuralSignal);
                values[checked((y * size.Width) + x)] = (float)detail;
            }
        }

        if (settings.SmoothingRadius > 0)
        {
            progress?.Report(new DetailAnalysisProgress(
                DetailAnalysisStage.Smoothing,
                0,
                size.Height,
                0.72d));
            values = BoxBlur(
                values,
                size,
                settings.SmoothingRadius,
                progress,
                cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new DetailAnalysisProgress(
            DetailAnalysisStage.Completed,
            size.Height,
            size.Height,
            1d));
        return new DetailMap(size.Width, size.Height, values);
    }

    private static Rgba32[] ReadPixels(
        IRgbaPixelSource source,
        CancellationToken cancellationToken)
    {
        ImageSize size = source.Size;
        Rgba32[] pixels = new Rgba32[checked((int)size.PixelCount)];

        for (int y = 0; y < size.Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            double normalizedY = (y + 0.5d) / size.Height;

            for (int x = 0; x < size.Width; x++)
            {
                double normalizedX = (x + 0.5d) / size.Width;
                pixels[checked((y * size.Width) + x)] = source.SampleNearest(
                    new NormalizedPoint(normalizedX, normalizedY));
            }
        }

        return pixels;
    }

    private static double CalculateEdgeSignal(
        Rgba32[] pixels,
        ImageSize size,
        int x,
        int y)
    {
        double left = Luminance(GetPixel(pixels, size, x - 1, y));
        double right = Luminance(GetPixel(pixels, size, x + 1, y));
        double top = Luminance(GetPixel(pixels, size, x, y - 1));
        double bottom = Luminance(GetPixel(pixels, size, x, y + 1));
        double horizontal = (right - left) * 0.5d;
        double vertical = (bottom - top) * 0.5d;
        return Math.Clamp(Math.Sqrt((horizontal * horizontal) + (vertical * vertical)), 0d, 1d);
    }

    private static double CalculateContrastSignal(
        Rgba32[] pixels,
        ImageSize size,
        int x,
        int y)
    {
        Rgba32 center = GetPixel(pixels, size, x, y);
        double total = ColorDistance(center, GetPixel(pixels, size, x - 1, y))
            + ColorDistance(center, GetPixel(pixels, size, x + 1, y))
            + ColorDistance(center, GetPixel(pixels, size, x, y - 1))
            + ColorDistance(center, GetPixel(pixels, size, x, y + 1));
        return Math.Clamp(total / 4d, 0d, 1d);
    }

    private static Rgba32 GetPixel(
        Rgba32[] pixels,
        ImageSize size,
        int x,
        int y)
    {
        int clampedX = Math.Clamp(x, 0, size.Width - 1);
        int clampedY = Math.Clamp(y, 0, size.Height - 1);
        return pixels[checked((clampedY * size.Width) + clampedX)];
    }

    private static double Luminance(Rgba32 color)
    {
        return ((0.2126d * color.Red)
            + (0.7152d * color.Green)
            + (0.0722d * color.Blue)) / byte.MaxValue;
    }

    private static double ColorDistance(Rgba32 first, Rgba32 second)
    {
        int red = first.Red - second.Red;
        int green = first.Green - second.Green;
        int blue = first.Blue - second.Blue;
        return Math.Sqrt((red * red) + (green * green) + (blue * blue))
            / MaximumRgbDistance;
    }

    private static float[] BoxBlur(
        float[] source,
        ImageSize size,
        int radius,
        IProgress<DetailAnalysisProgress>? progress,
        CancellationToken cancellationToken)
    {
        float[] horizontal = new float[source.Length];
        float[] result = new float[source.Length];

        for (int y = 0; y < size.Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int rowOffset = checked(y * size.Width);
            double sum = 0d;
            int start = 0;
            int end = Math.Min(size.Width - 1, radius);

            for (int x = start; x <= end; x++)
            {
                sum += source[rowOffset + x];
            }

            for (int x = 0; x < size.Width; x++)
            {
                int currentStart = Math.Max(0, x - radius);
                int currentEnd = Math.Min(size.Width - 1, x + radius);
                horizontal[rowOffset + x] = (float)(sum / (currentEnd - currentStart + 1));

                int removeIndex = x - radius;
                int addIndex = x + radius + 1;
                if (removeIndex >= 0)
                {
                    sum -= source[rowOffset + removeIndex];
                }

                if (addIndex < size.Width)
                {
                    sum += source[rowOffset + addIndex];
                }
            }
        }

        double[] columnSums = new double[size.Width];
        int initialEnd = Math.Min(size.Height - 1, radius);
        for (int y = 0; y <= initialEnd; y++)
        {
            int rowOffset = checked(y * size.Width);
            for (int x = 0; x < size.Width; x++)
            {
                columnSums[x] += horizontal[rowOffset + x];
            }
        }

        for (int y = 0; y < size.Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (y % ProgressRowBatch == 0)
            {
                progress?.Report(new DetailAnalysisProgress(
                    DetailAnalysisStage.Smoothing,
                    y,
                    size.Height,
                    0.72d + (0.26d * y / size.Height)));
            }

            int currentStart = Math.Max(0, y - radius);
            int currentEnd = Math.Min(size.Height - 1, y + radius);
            int count = currentEnd - currentStart + 1;
            int rowOffset = checked(y * size.Width);
            for (int x = 0; x < size.Width; x++)
            {
                result[rowOffset + x] = (float)(columnSums[x] / count);
            }

            int removeRow = y - radius;
            int addRow = y + radius + 1;
            if (removeRow >= 0)
            {
                int removeOffset = checked(removeRow * size.Width);
                for (int x = 0; x < size.Width; x++)
                {
                    columnSums[x] -= horizontal[removeOffset + x];
                }
            }

            if (addRow < size.Height)
            {
                int addOffset = checked(addRow * size.Width);
                for (int x = 0; x < size.Width; x++)
                {
                    columnSums[x] += horizontal[addOffset + x];
                }
            }
        }

        return result;
    }
}
