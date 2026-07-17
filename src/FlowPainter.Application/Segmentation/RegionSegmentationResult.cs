using System.Collections.ObjectModel;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Segmentation;

public sealed class RegionSegmentationResult
{
    public RegionSegmentationResult(
        RegionLabelMap labels,
        IEnumerable<ImageRegion> regions,
        RegionAdjacencyGraph adjacency,
        RegionHierarchy hierarchy,
        SegmentationDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(regions);
        ArgumentNullException.ThrowIfNull(adjacency);
        ArgumentNullException.ThrowIfNull(hierarchy);
        ArgumentNullException.ThrowIfNull(diagnostics);

        ImageRegion[] regionArray = regions.OrderBy(region => region.Id).ToArray();
        if (regionArray.Length != labels.RegionCount)
        {
            throw new ArgumentException("The region list must contain one entry for every compact label.", nameof(regions));
        }

        int[] labelCounts = labels.CountPixelsByRegion();
        long totalArea = 0L;
        for (int regionId = 0; regionId < regionArray.Length; regionId++)
        {
            ImageRegion region = regionArray[regionId];
            if (region.Id != regionId)
            {
                throw new ArgumentException("Region identifiers must be compact and ordered from zero.", nameof(regions));
            }

            if (region.PixelCount != labelCounts[regionId])
            {
                throw new ArgumentException("Region pixel counts must match the label map.", nameof(regions));
            }

            double expectedArea = region.PixelCount / (double)labels.Size.PixelCount;
            if (Math.Abs(region.NormalizedArea - expectedArea) > 1e-12d)
            {
                throw new ArgumentException("Region normalized areas must match the label map.", nameof(regions));
            }

            if (region.Bounds.Right > labels.Size.Width || region.Bounds.Bottom > labels.Size.Height)
            {
                throw new ArgumentException("Region bounds must lie inside the label map.", nameof(regions));
            }

            totalArea = checked(totalArea + region.PixelCount);
        }

        if (totalArea != labels.Size.PixelCount)
        {
            throw new ArgumentException("The sum of region areas must cover the complete label map.", nameof(regions));
        }

        if (adjacency.RegionCount != labels.RegionCount)
        {
            throw new ArgumentException("The adjacency graph must use the label-map region count.", nameof(adjacency));
        }

        ValidateAdjacency(labels, adjacency);

        if (hierarchy.FineRegionCount != labels.RegionCount)
        {
            throw new ArgumentException("The hierarchy must map every fine label.", nameof(hierarchy));
        }

        if (diagnostics.FinalRegionCount != labels.RegionCount)
        {
            throw new ArgumentException("Diagnostics must report the final label-map region count.", nameof(diagnostics));
        }

        if (diagnostics.RegionSizes is not null)
        {
            RegionSizeDistribution expectedDistribution = RegionSizeDistribution.Create(labelCounts);
            RegionSizeDistribution actualDistribution = diagnostics.RegionSizes;
            if (actualDistribution.MinimumPixelCount != expectedDistribution.MinimumPixelCount
                || actualDistribution.MaximumPixelCount != expectedDistribution.MaximumPixelCount
                || Math.Abs(actualDistribution.MeanPixelCount - expectedDistribution.MeanPixelCount) > 1e-12d
                || Math.Abs(
                    actualDistribution.StandardDeviationPixelCount
                    - expectedDistribution.StandardDeviationPixelCount) > 1e-12d)
            {
                throw new ArgumentException(
                    "Diagnostic region-size statistics must match the label map.",
                    nameof(diagnostics));
            }
        }

        Labels = labels;
        Regions = Array.AsReadOnly(regionArray);
        Adjacency = adjacency;
        Hierarchy = hierarchy;
        Diagnostics = diagnostics;
    }

    public RegionLabelMap Labels { get; }

    public ReadOnlyCollection<ImageRegion> Regions { get; }

    public RegionAdjacencyGraph Adjacency { get; }

    public RegionHierarchy Hierarchy { get; }

    public SegmentationDiagnostics Diagnostics { get; }

    private static void ValidateAdjacency(
        RegionLabelMap labels,
        RegionAdjacencyGraph adjacency)
    {
        Dictionary<(int First, int Second), int> expectedLengths = new();
        for (int y = 0; y < labels.Size.Height; y++)
        {
            RegionLabelRow row = labels.GetRow(y);
            for (int x = 0; x + 1 < labels.Size.Width; x++)
            {
                AddExpectedBoundary(expectedLengths, row[x], row[x + 1]);
            }

            if (y + 1 >= labels.Size.Height)
            {
                continue;
            }

            RegionLabelRow nextRow = labels.GetRow(y + 1);
            for (int x = 0; x < labels.Size.Width; x++)
            {
                AddExpectedBoundary(expectedLengths, row[x], nextRow[x]);
            }
        }

        if (adjacency.Edges.Count != expectedLengths.Count)
        {
            throw new ArgumentException(
                "The adjacency graph must contain exactly one edge for every adjacent label pair.",
                nameof(adjacency));
        }

        foreach (KeyValuePair<(int First, int Second), int> pair in expectedLengths)
        {
            if (!adjacency.TryGetEdge(pair.Key.First, pair.Key.Second, out RegionAdjacency? edge)
                || edge is null
                || edge.SharedBoundaryLength != pair.Value)
            {
                throw new ArgumentException(
                    "Adjacency edges and shared-boundary lengths must match the label map.",
                    nameof(adjacency));
            }
        }
    }

    private static void AddExpectedBoundary(
        Dictionary<(int First, int Second), int> expectedLengths,
        uint firstLabel,
        uint secondLabel)
    {
        if (firstLabel == secondLabel)
        {
            return;
        }

        uint lowerLabel = firstLabel < secondLabel ? firstLabel : secondLabel;
        uint higherLabel = firstLabel < secondLabel ? secondLabel : firstLabel;
        int firstRegionId = checked((int)lowerLabel);
        int secondRegionId = checked((int)higherLabel);
        (int First, int Second) key = (firstRegionId, secondRegionId);
        expectedLengths.TryGetValue(key, out int currentLength);
        expectedLengths[key] = checked(currentLength + 1);
    }
}
