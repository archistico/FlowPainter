using FlowPainter.Application.Segmentation;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Tests.Segmentation;

public sealed class RegionHierarchyBuilderTests
{
    [Fact]
    public void BuildCreatesFineIntermediateAndBroadMassLevels()
    {
        ImageRegion[] regions = CreateUniformRegions(4, 10d);
        RegionAdjacencyGraph graph = CreateChainGraph(4);
        RegionMergeSettings settings = CreatePermissiveSettings(0.5d, 0.25d);

        RegionHierarchy hierarchy = RegionHierarchyBuilder.Build(regions, graph, settings);

        Assert.Equal(3, hierarchy.Levels.Count);
        Assert.Equal(4, hierarchy.Levels[0].ParentRegionCount);
        Assert.Equal(2, hierarchy.Levels[1].ParentRegionCount);
        Assert.Equal(1, hierarchy.Levels[2].ParentRegionCount);
    }

    [Fact]
    public void BuildProtectsStrongBoundary()
    {
        ImageRegion[] regions = CreateUniformRegions(2, 10d);
        RegionAdjacencyGraph graph = new(
            2,
            new[] { new RegionAdjacency(0, 1, 4, boundaryStrength: 0.8d) });
        RegionMergeSettings settings = new(
            intermediateTargetRatio: 0.5d,
            broadMassTargetRatio: 0.5d,
            intermediateMaximumCost: 1d,
            broadMassMaximumCost: 1d,
            strongBoundaryThreshold: 0.7d,
            maximumParentAreaFraction: 1d);

        RegionHierarchy hierarchy = RegionHierarchyBuilder.Build(regions, graph, settings);

        Assert.Equal(2, hierarchy.Levels[1].ParentRegionCount);
        Assert.Equal(2, hierarchy.Levels[2].ParentRegionCount);
    }

    [Fact]
    public void BuildMergesOnlyAdjacentRegions()
    {
        ImageRegion[] regions = CreateUniformRegions(2, 10d);
        RegionAdjacencyGraph graph = RegionAdjacencyGraph.CreateEmpty(2);

        RegionHierarchy hierarchy = RegionHierarchyBuilder.Build(
            regions,
            graph,
            CreatePermissiveSettings(0.5d, 0.5d));

        Assert.Equal(2, hierarchy.Levels[1].ParentRegionCount);
    }

    [Fact]
    public void BuildStopsWhenBestCostExceedsLevelThreshold()
    {
        ImageRegion[] regions = CreateRegions(0d, 100d);
        RegionAdjacencyGraph graph = CreateChainGraph(2);
        RegionMergeSettings settings = new(
            intermediateTargetRatio: 0.5d,
            broadMassTargetRatio: 0.5d,
            intermediateMaximumCost: 0.05d,
            broadMassMaximumCost: 0.05d,
            strongBoundaryThreshold: 1d,
            maximumParentAreaFraction: 1d);

        RegionHierarchy hierarchy = RegionHierarchyBuilder.Build(regions, graph, settings);

        Assert.Equal(2, hierarchy.Levels[1].ParentRegionCount);
    }

    [Fact]
    public void BuildHonorsMaximumParentArea()
    {
        ImageRegion[] regions = CreateUniformRegions(3, 10d);
        RegionAdjacencyGraph graph = CreateChainGraph(3);
        RegionMergeSettings settings = new(
            intermediateTargetRatio: 0.34d,
            broadMassTargetRatio: 0.34d,
            intermediateMaximumCost: 1d,
            broadMassMaximumCost: 1d,
            strongBoundaryThreshold: 1d,
            maximumParentAreaFraction: 0.5d);

        RegionHierarchy hierarchy = RegionHierarchyBuilder.Build(regions, graph, settings);

        Assert.Equal(3, hierarchy.Levels[1].ParentRegionCount);
    }

    [Fact]
    public void BuildRecomputesCostAfterAcceptedMerge()
    {
        ImageRegion[] regions = CreateRegions(0d, 0d, 100d);
        RegionAdjacencyGraph graph = CreateChainGraph(3);
        RegionMergeSettings settings = new(
            intermediateTargetRatio: 0.34d,
            broadMassTargetRatio: 0.34d,
            intermediateMaximumCost: 0.2d,
            broadMassMaximumCost: 0.2d,
            strongBoundaryThreshold: 1d,
            maximumParentAreaFraction: 1d);

        RegionHierarchy hierarchy = RegionHierarchyBuilder.Build(regions, graph, settings);

        Assert.Equal(2, hierarchy.Levels[1].ParentRegionCount);
        Assert.Equal(
            hierarchy.Levels[1].GetParentId(0),
            hierarchy.Levels[1].GetParentId(1));
        Assert.NotEqual(
            hierarchy.Levels[1].GetParentId(1),
            hierarchy.Levels[1].GetParentId(2));
    }

    [Fact]
    public void BuildUsesStableIdentifiersForEqualCostTies()
    {
        ImageRegion[] regions = CreateUniformRegions(4, 10d);
        RegionAdjacencyGraph graph = CreateChainGraph(4);
        RegionMergeSettings settings = CreatePermissiveSettings(0.5d, 0.5d);

        RegionHierarchy hierarchy = RegionHierarchyBuilder.Build(regions, graph, settings);
        RegionHierarchyLevel intermediate = hierarchy.Levels[1];

        Assert.Equal(intermediate.GetParentId(0), intermediate.GetParentId(1));
        Assert.Equal(intermediate.GetParentId(2), intermediate.GetParentId(3));
        Assert.NotEqual(intermediate.GetParentId(1), intermediate.GetParentId(2));
    }

    [Fact]
    public void BuildPreservesStrongBoundaryAfterNeighbourMerge()
    {
        ImageRegion[] regions = CreateUniformRegions(3, 10d);
        RegionAdjacencyGraph graph = new(
            3,
            new[]
            {
                new RegionAdjacency(0, 1, 4, boundaryStrength: 0.05d),
                new RegionAdjacency(1, 2, 4, boundaryStrength: 0.9d),
            });
        RegionMergeSettings settings = new(
            intermediateTargetRatio: 0.34d,
            broadMassTargetRatio: 0.34d,
            intermediateMaximumCost: 1d,
            broadMassMaximumCost: 1d,
            strongBoundaryThreshold: 0.7d,
            maximumParentAreaFraction: 1d);

        RegionHierarchy hierarchy = RegionHierarchyBuilder.Build(regions, graph, settings);

        Assert.Equal(2, hierarchy.Levels[1].ParentRegionCount);
        Assert.Equal(
            hierarchy.Levels[1].GetParentId(0),
            hierarchy.Levels[1].GetParentId(1));
        Assert.NotEqual(
            hierarchy.Levels[1].GetParentId(1),
            hierarchy.Levels[1].GetParentId(2));
    }

    [Fact]
    public void BuildIsDeterministic()
    {
        ImageRegion[] regions = CreateUniformRegions(5, 10d);
        RegionAdjacencyGraph graph = CreateChainGraph(5);
        RegionMergeSettings settings = CreatePermissiveSettings(0.6d, 0.4d);

        RegionHierarchy first = RegionHierarchyBuilder.Build(regions, graph, settings);
        RegionHierarchy second = RegionHierarchyBuilder.Build(regions, graph, settings);

        Assert.Equal(
            first.Levels[1].CopyParentIds(),
            second.Levels[1].CopyParentIds());
        Assert.Equal(
            first.Levels[2].CopyParentIds(),
            second.Levels[2].CopyParentIds());
    }

    [Fact]
    public void BuildObservesCancellation()
    {
        ImageRegion[] regions = CreateUniformRegions(2, 10d);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => RegionHierarchyBuilder.Build(
            regions,
            CreateChainGraph(2),
            CreatePermissiveSettings(0.5d, 0.5d),
            cancellation.Token));
    }

    [Fact]
    public void BuildRejectsGraphRegionCountMismatch()
    {
        ImageRegion[] regions = CreateUniformRegions(2, 10d);

        Assert.Throws<ArgumentException>(() => RegionHierarchyBuilder.Build(
            regions,
            RegionAdjacencyGraph.CreateEmpty(1),
            new RegionMergeSettings()));
    }

    private static RegionMergeSettings CreatePermissiveSettings(
        double intermediateTargetRatio,
        double broadMassTargetRatio)
    {
        return new RegionMergeSettings(
            intermediateTargetRatio,
            broadMassTargetRatio,
            1d,
            1d,
            1d,
            1d);
    }

    private static ImageRegion[] CreateUniformRegions(int count, double meanLightness)
    {
        return CreateRegions(count, _ => meanLightness);
    }

    private static ImageRegion[] CreateRegions(double firstLightness, double secondLightness)
    {
        return CreateRegions(
            2,
            regionId => regionId == 0 ? firstLightness : secondLightness);
    }

    private static ImageRegion[] CreateRegions(
        double firstLightness,
        double secondLightness,
        double thirdLightness)
    {
        return CreateRegions(
            3,
            regionId => regionId switch
            {
                0 => firstLightness,
                1 => secondLightness,
                _ => thirdLightness,
            });
    }

    private static ImageRegion[] CreateRegions(
        int count,
        Func<int, double> lightnessSelector)
    {
        double normalizedArea = 1d / count;
        ImageRegion[] regions = new ImageRegion[count];
        for (int regionId = 0; regionId < regions.Length; regionId++)
        {
            regions[regionId] = new ImageRegion(
                regionId,
                25,
                normalizedArea,
                new PixelBounds(regionId, 0, regionId + 1, 1),
                new RegionCentroid(regionId + 0.5d, 0.5d),
                new RegionVisualDescriptors(
                    perimeter: 20d,
                    compactness: 0.7d,
                    meanLightness: lightnessSelector(regionId),
                    textureEnergy: 2d));
        }

        return regions;
    }

    private static RegionAdjacencyGraph CreateChainGraph(int regionCount)
    {
        List<RegionAdjacency> edges = new(regionCount - 1);
        for (int regionId = 0; regionId + 1 < regionCount; regionId++)
        {
            edges.Add(new RegionAdjacency(regionId, regionId + 1, 4));
        }

        return new RegionAdjacencyGraph(regionCount, edges);
    }
}
