using FlowPainter.Application.Segmentation;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Tests.Boundaries;

internal static class RegionalBoundaryTestFactory
{
    public static RegionSegmentationResult CreateVerticalSplit(
        int width,
        int height,
        int splitX,
        double boundaryStrength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(splitX, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(splitX, width);
        ImageSize size = new(width, height);
        uint[] labels = new uint[checked((int)size.PixelCount)];
        for (int y = 0; y < height; y++)
        {
            for (int x = splitX; x < width; x++)
            {
                labels[(y * width) + x] = 1;
            }
        }

        RegionAdjacency edge = new(
            0,
            1,
            height,
            prevailingTangentRadians: Math.PI / 2d,
            boundaryStrength: boundaryStrength);
        return Create(size, 2, labels, [edge]);
    }

    public static RegionSegmentationResult CreateHorizontalSplit(
        int width,
        int height,
        int splitY,
        double boundaryStrength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(splitY, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(splitY, height);
        ImageSize size = new(width, height);
        uint[] labels = new uint[checked((int)size.PixelCount)];
        for (int y = splitY; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                labels[(y * width) + x] = 1;
            }
        }

        RegionAdjacency edge = new(
            0,
            1,
            width,
            prevailingTangentRadians: 0d,
            boundaryStrength: boundaryStrength);
        return Create(size, 2, labels, [edge]);
    }

    public static RegionSegmentationResult CreateThreeVerticalBands(
        int width,
        int height,
        int firstSplitX,
        int secondSplitX,
        double firstBoundaryStrength,
        double secondBoundaryStrength)
    {
        if (firstSplitX < 1 || secondSplitX <= firstSplitX || secondSplitX >= width)
        {
            throw new ArgumentException(
                "The vertical split positions must be ordered and lie inside the image.",
                nameof(secondSplitX));
        }

        ImageSize size = new(width, height);
        uint[] labels = new uint[checked((int)size.PixelCount)];
        for (int y = 0; y < height; y++)
        {
            for (int x = firstSplitX; x < secondSplitX; x++)
            {
                labels[(y * width) + x] = 1;
            }

            for (int x = secondSplitX; x < width; x++)
            {
                labels[(y * width) + x] = 2;
            }
        }

        RegionAdjacency[] edges =
        [
            new RegionAdjacency(
                0,
                1,
                height,
                prevailingTangentRadians: Math.PI / 2d,
                boundaryStrength: firstBoundaryStrength),
            new RegionAdjacency(
                1,
                2,
                height,
                prevailingTangentRadians: Math.PI / 2d,
                boundaryStrength: secondBoundaryStrength),
        ];
        return Create(size, 3, labels, edges);
    }

    public static RegionSegmentationResult CreateSingleRegion(int width, int height)
    {
        ImageSize size = new(width, height);
        uint[] labels = new uint[checked((int)size.PixelCount)];
        return Create(size, 1, labels, Array.Empty<RegionAdjacency>());
    }

    private static RegionSegmentationResult Create(
        ImageSize size,
        int regionCount,
        uint[] labels,
        RegionAdjacency[] edges)
    {
        RegionLabelMap labelMap = RegionLabelMap.Create(size, regionCount, labels);
        ImageRegion[] regions = CreateRegions(size, regionCount, labels);
        return new RegionSegmentationResult(
            labelMap,
            regions,
            new RegionAdjacencyGraph(regionCount, edges),
            RegionHierarchy.CreateIdentity(regionCount),
            new SegmentationDiagnostics(0, true, 0d, regionCount, regionCount));
    }

    private static ImageRegion[] CreateRegions(
        ImageSize size,
        int regionCount,
        uint[] labels)
    {
        int[] counts = new int[regionCount];
        int[] minimumX = Enumerable.Repeat(int.MaxValue, regionCount).ToArray();
        int[] minimumY = Enumerable.Repeat(int.MaxValue, regionCount).ToArray();
        int[] maximumX = Enumerable.Repeat(int.MinValue, regionCount).ToArray();
        int[] maximumY = Enumerable.Repeat(int.MinValue, regionCount).ToArray();
        double[] sumX = new double[regionCount];
        double[] sumY = new double[regionCount];

        for (int y = 0; y < size.Height; y++)
        {
            for (int x = 0; x < size.Width; x++)
            {
                int regionId = checked((int)labels[(y * size.Width) + x]);
                counts[regionId]++;
                minimumX[regionId] = Math.Min(minimumX[regionId], x);
                minimumY[regionId] = Math.Min(minimumY[regionId], y);
                maximumX[regionId] = Math.Max(maximumX[regionId], x);
                maximumY[regionId] = Math.Max(maximumY[regionId], y);
                sumX[regionId] += x;
                sumY[regionId] += y;
            }
        }

        ImageRegion[] regions = new ImageRegion[regionCount];
        for (int regionId = 0; regionId < regionCount; regionId++)
        {
            regions[regionId] = new ImageRegion(
                regionId,
                counts[regionId],
                counts[regionId] / (double)size.PixelCount,
                new PixelBounds(
                    minimumX[regionId],
                    minimumY[regionId],
                    maximumX[regionId] + 1,
                    maximumY[regionId] + 1),
                new RegionCentroid(
                    sumX[regionId] / counts[regionId],
                    sumY[regionId] / counts[regionId]));
        }

        return regions;
    }
}
