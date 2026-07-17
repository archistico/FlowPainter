using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Segmentation;
using FlowPainter.Application.Workloads;

namespace FlowPainter.Application.Segmentation;

public static class SegmentationDiagnosticRenderer
{
    private const int CancellationRowBatch = 16;
    private static readonly Rgba32 DefaultBoundaryColor = Rgba32.Opaque(36, 123, 255);

    public static RgbaImage CreateMeanColorPreview(
        IRgbaPixelSource source,
        RegionLabelMap labels,
        CancellationToken cancellationToken = default)
    {
        ValidateInputs(source, labels);
        long outputBytes = labels.Size.GetRequiredBytes(ImageSize.RgbaBytesPerPixel);
        long statisticsBytes = checked((long)labels.RegionCount * 40L);
        WorkloadBudgetPolicy.EnsureMemoryWithinBudget(
            checked(outputBytes + statisticsBytes),
            "segmentation mean-colour diagnostic preview");
        int regionCount = labels.RegionCount;
        long[] redSums = new long[regionCount];
        long[] greenSums = new long[regionCount];
        long[] blueSums = new long[regionCount];
        long[] alphaSums = new long[regionCount];
        int[] counts = new int[regionCount];

        for (int y = 0; y < labels.Size.Height; y++)
        {
            if (y % CancellationRowBatch == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            RegionLabelRow row = labels.GetRow(y);
            for (int x = 0; x < labels.Size.Width; x++)
            {
                int regionId = checked((int)row[x]);
                Rgba32 pixel = Sample(source, x, y);
                redSums[regionId] += pixel.Red;
                greenSums[regionId] += pixel.Green;
                blueSums[regionId] += pixel.Blue;
                alphaSums[regionId] += pixel.Alpha;
                counts[regionId]++;
            }
        }

        Rgba32[] means = new Rgba32[regionCount];
        for (int regionId = 0; regionId < regionCount; regionId++)
        {
            int count = counts[regionId];
            means[regionId] = new Rgba32(
                DivideRounded(redSums[regionId], count),
                DivideRounded(greenSums[regionId], count),
                DivideRounded(blueSums[regionId], count),
                DivideRounded(alphaSums[regionId], count));
        }

        Rgba32[] output = new Rgba32[checked((int)labels.Size.PixelCount)];
        for (int y = 0; y < labels.Size.Height; y++)
        {
            if (y % CancellationRowBatch == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            RegionLabelRow row = labels.GetRow(y);
            int rowOffset = checked(y * labels.Size.Width);
            for (int x = 0; x < labels.Size.Width; x++)
            {
                output[rowOffset + x] = means[checked((int)row[x])];
            }
        }

        return new RgbaImage(labels.Size, output);
    }

    public static RgbaImage CreateBoundaryOverlay(
        IRgbaPixelSource source,
        RegionLabelMap labels,
        Rgba32? boundaryColor = null,
        CancellationToken cancellationToken = default)
    {
        ValidateInputs(source, labels);
        WorkloadBudgetPolicy.EnsureMemoryWithinBudget(
            labels.Size.GetRequiredBytes(ImageSize.RgbaBytesPerPixel),
            "segmentation boundary diagnostic overlay");
        Rgba32 color = boundaryColor ?? DefaultBoundaryColor;
        Rgba32[] output = new Rgba32[checked((int)labels.Size.PixelCount)];

        for (int y = 0; y < labels.Size.Height; y++)
        {
            if (y % CancellationRowBatch == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            int rowOffset = checked(y * labels.Size.Width);
            for (int x = 0; x < labels.Size.Width; x++)
            {
                output[rowOffset + x] = IsBoundary(labels, x, y)
                    ? color
                    : Sample(source, x, y);
            }
        }

        return new RgbaImage(labels.Size, output);
    }


    public static RgbaImage CreateHierarchyMeanColorPreview(
        IRgbaPixelSource source,
        RegionLabelMap labels,
        RegionHierarchy hierarchy,
        int level,
        CancellationToken cancellationToken = default)
    {
        ValidateInputs(source, labels);
        ArgumentNullException.ThrowIfNull(hierarchy);
        if (hierarchy.FineRegionCount != labels.RegionCount)
        {
            throw new ArgumentException(
                "The hierarchy and label map must use the same fine-region count.",
                nameof(hierarchy));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(level);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(level, hierarchy.Levels.Count);
        RegionHierarchyLevel hierarchyLevel = hierarchy.Levels[level];
        long parentLabelBytes = labels.Size.GetRequiredBytes(sizeof(uint));
        long outputBytes = labels.Size.GetRequiredBytes(ImageSize.RgbaBytesPerPixel);
        long statisticsBytes = checked((long)hierarchyLevel.ParentRegionCount * 40L);
        WorkloadBudgetPolicy.EnsureMemoryWithinBudget(
            checked(parentLabelBytes + outputBytes + statisticsBytes),
            "hierarchical segmentation diagnostic preview");
        uint[] parentLabels = new uint[checked((int)labels.Size.PixelCount)];
        for (int y = 0; y < labels.Size.Height; y++)
        {
            if (y % CancellationRowBatch == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            RegionLabelRow row = labels.GetRow(y);
            int rowOffset = checked(y * labels.Size.Width);
            for (int x = 0; x < labels.Size.Width; x++)
            {
                parentLabels[rowOffset + x] = checked((uint)hierarchyLevel.GetParentId(
                    checked((int)row[x])));
            }
        }

        RegionLabelMap parentMap = RegionLabelMap.Create(
            labels.Size,
            hierarchyLevel.ParentRegionCount,
            parentLabels);
        return CreateMeanColorPreview(source, parentMap, cancellationToken);
    }

    public static RgbaImage CreateStrongBoundaryOverlay(
        IRgbaPixelSource source,
        RegionLabelMap labels,
        RegionAdjacencyGraph adjacency,
        double minimumStrength,
        Rgba32? boundaryColor = null,
        CancellationToken cancellationToken = default)
    {
        ValidateInputs(source, labels);
        ArgumentNullException.ThrowIfNull(adjacency);
        if (adjacency.RegionCount != labels.RegionCount)
        {
            throw new ArgumentException(
                "The adjacency graph and label map must use the same region count.",
                nameof(adjacency));
        }

        if (!double.IsFinite(minimumStrength) || minimumStrength < 0d || minimumStrength > 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumStrength),
                minimumStrength,
                "The minimum strength must be finite and between zero and one.");
        }

        long outputBytes = labels.Size.GetRequiredBytes(ImageSize.RgbaBytesPerPixel);
        long edgeLookupBytes = checked((long)adjacency.Edges.Count * 32L);
        WorkloadBudgetPolicy.EnsureMemoryWithinBudget(
            checked(outputBytes + edgeLookupBytes),
            "strong regional-boundary diagnostic overlay");
        HashSet<ulong> strongPairs = adjacency.Edges
            .Where(edge => edge.BoundaryStrength >= minimumStrength)
            .Select(edge => CreatePairKey(edge.FirstRegionId, edge.SecondRegionId))
            .ToHashSet();
        Rgba32 color = boundaryColor ?? DefaultBoundaryColor;
        Rgba32[] output = new Rgba32[checked((int)labels.Size.PixelCount)];

        for (int y = 0; y < labels.Size.Height; y++)
        {
            if (y % CancellationRowBatch == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            int rowOffset = checked(y * labels.Size.Width);
            for (int x = 0; x < labels.Size.Width; x++)
            {
                output[rowOffset + x] = IsStrongBoundary(labels, strongPairs, x, y)
                    ? color
                    : Sample(source, x, y);
            }
        }

        return new RgbaImage(labels.Size, output);
    }

    private static bool IsBoundary(RegionLabelMap labels, int x, int y)
    {
        uint label = labels[x, y];
        return (x > 0 && labels[x - 1, y] != label)
            || (x + 1 < labels.Size.Width && labels[x + 1, y] != label)
            || (y > 0 && labels[x, y - 1] != label)
            || (y + 1 < labels.Size.Height && labels[x, y + 1] != label);
    }

    private static bool IsStrongBoundary(
        RegionLabelMap labels,
        HashSet<ulong> strongPairs,
        int x,
        int y)
    {
        int regionId = checked((int)labels[x, y]);
        return (x > 0 && IsStrongPair(strongPairs, regionId, checked((int)labels[x - 1, y])))
            || (x + 1 < labels.Size.Width
                && IsStrongPair(strongPairs, regionId, checked((int)labels[x + 1, y])))
            || (y > 0 && IsStrongPair(strongPairs, regionId, checked((int)labels[x, y - 1])))
            || (y + 1 < labels.Size.Height
                && IsStrongPair(strongPairs, regionId, checked((int)labels[x, y + 1])));
    }

    private static bool IsStrongPair(HashSet<ulong> strongPairs, int firstRegionId, int secondRegionId)
    {
        return firstRegionId != secondRegionId
            && strongPairs.Contains(CreatePairKey(firstRegionId, secondRegionId));
    }

    private static ulong CreatePairKey(int firstRegionId, int secondRegionId)
    {
        uint lower = checked((uint)Math.Min(firstRegionId, secondRegionId));
        uint higher = checked((uint)Math.Max(firstRegionId, secondRegionId));
        return ((ulong)lower << 32) | higher;
    }

    private static Rgba32 Sample(IRgbaPixelSource source, int x, int y)
    {
        return source.SampleNearest(new NormalizedPoint(
            (x + 0.5d) / source.Size.Width,
            (y + 0.5d) / source.Size.Height));
    }

    private static byte DivideRounded(long value, int count)
    {
        return checked((byte)((value + (count / 2L)) / count));
    }

    private static void ValidateInputs(IRgbaPixelSource source, RegionLabelMap labels)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(labels);
        if (source.Size != labels.Size)
        {
            throw new ArgumentException(
                "The source and label map must have identical dimensions.",
                nameof(labels));
        }
    }
}
