using FlowPainter.Application.Segmentation;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Tests.Segmentation;

public sealed class RegionSegmentationResultTests
{
    [Fact]
    public void ConstructorAcceptsConsistentImmutableResult()
    {
        RegionSegmentationResult result = CreateValidResult();

        Assert.Equal(2, result.Regions.Count);
        Assert.Equal(2, result.Labels.RegionCount);
        Assert.Single(result.Adjacency.Edges);
        Assert.Single(result.Hierarchy.Levels);
    }

    [Fact]
    public void ConstructorCopiesAndOrdersRegions()
    {
        RegionLabelMap labels = CreateLabels();
        ImageRegion first = CreateRegion(0, 0, 1);
        ImageRegion second = CreateRegion(1, 1, 2);
        List<ImageRegion> regions = new() { second, first };

        RegionSegmentationResult result = new(
            labels,
            regions,
            CreateGraph(),
            RegionHierarchy.CreateIdentity(2),
            CreateDiagnostics());
        regions.Clear();

        Assert.Equal(0, result.Regions[0].Id);
        Assert.Equal(1, result.Regions[1].Id);
    }

    [Fact]
    public void ConstructorRejectsRegionCountMismatch()
    {
        RegionLabelMap labels = CreateLabels();

        Assert.Throws<ArgumentException>(() => new RegionSegmentationResult(
            labels,
            new[] { CreateRegion(0, 0, 1) },
            CreateGraph(),
            RegionHierarchy.CreateIdentity(2),
            CreateDiagnostics()));
    }

    [Fact]
    public void ConstructorRejectsPixelCountMismatch()
    {
        RegionLabelMap labels = CreateLabels();
        ImageRegion invalid = new(
            0,
            1,
            0.25d,
            new PixelBounds(0, 0, 1, 2),
            new RegionCentroid(0d, 0.5d));

        Assert.Throws<ArgumentException>(() => new RegionSegmentationResult(
            labels,
            new[] { invalid, CreateRegion(1, 1, 2) },
            CreateGraph(),
            RegionHierarchy.CreateIdentity(2),
            CreateDiagnostics()));
    }

    [Fact]
    public void ConstructorRejectsAdjacencyRegionCountMismatch()
    {
        RegionLabelMap labels = CreateLabels();

        Assert.Throws<ArgumentException>(() => new RegionSegmentationResult(
            labels,
            CreateRegions(),
            RegionAdjacencyGraph.CreateEmpty(3),
            RegionHierarchy.CreateIdentity(2),
            CreateDiagnostics()));
    }


    [Fact]
    public void ConstructorRejectsIncompleteAdjacency()
    {
        RegionLabelMap labels = CreateLabels();

        Assert.Throws<ArgumentException>(() => new RegionSegmentationResult(
            labels,
            CreateRegions(),
            RegionAdjacencyGraph.CreateEmpty(2),
            RegionHierarchy.CreateIdentity(2),
            CreateDiagnostics()));
    }

    [Fact]
    public void ConstructorRejectsIncorrectSharedBoundaryLength()
    {
        RegionLabelMap labels = CreateLabels();
        RegionAdjacency[] edges = [new RegionAdjacency(0, 1, 1)];
        RegionAdjacencyGraph adjacency = new(2, edges);

        Assert.Throws<ArgumentException>(() => new RegionSegmentationResult(
            labels,
            CreateRegions(),
            adjacency,
            RegionHierarchy.CreateIdentity(2),
            CreateDiagnostics()));
    }

    [Fact]
    public void ConstructorRejectsHierarchyRegionCountMismatch()
    {
        RegionLabelMap labels = CreateLabels();

        Assert.Throws<ArgumentException>(() => new RegionSegmentationResult(
            labels,
            CreateRegions(),
            CreateGraph(),
            RegionHierarchy.CreateIdentity(3),
            CreateDiagnostics()));
    }

    [Fact]
    public void ConstructorRejectsDiagnosticRegionCountMismatch()
    {
        RegionLabelMap labels = CreateLabels();

        Assert.Throws<ArgumentException>(() => new RegionSegmentationResult(
            labels,
            CreateRegions(),
            CreateGraph(),
            RegionHierarchy.CreateIdentity(2),
            new SegmentationDiagnostics(0, false, 0d, 3, 3)));
        Assert.Throws<ArgumentException>(() => new RegionSegmentationResult(
            labels,
            CreateRegions(),
            CreateGraph(),
            RegionHierarchy.CreateIdentity(2),
            new SegmentationDiagnostics(
                0,
                false,
                0d,
                2,
                2,
                regionSizes: new RegionSizeDistribution(1, 3, 2d, 1d))));
    }

    private static RegionSegmentationResult CreateValidResult()
    {
        return new RegionSegmentationResult(
            CreateLabels(),
            CreateRegions(),
            CreateGraph(),
            RegionHierarchy.CreateIdentity(2),
            CreateDiagnostics());
    }

    private static RegionLabelMap CreateLabels()
    {
        return RegionLabelMap.Create(
            new ImageSize(2, 2),
            2,
            new uint[] { 0, 1, 0, 1 });
    }

    private static ImageRegion[] CreateRegions()
    {
        return new[] { CreateRegion(0, 0, 1), CreateRegion(1, 1, 2) };
    }

    private static ImageRegion CreateRegion(int id, int left, int right)
    {
        return new ImageRegion(
            id,
            2,
            0.5d,
            new PixelBounds(left, 0, right, 2),
            new RegionCentroid(left, 0.5d));
    }

    private static RegionAdjacencyGraph CreateGraph()
    {
        return new RegionAdjacencyGraph(2, new[] { new RegionAdjacency(0, 1, 2) });
    }

    private static SegmentationDiagnostics CreateDiagnostics()
    {
        return new SegmentationDiagnostics(0, false, 0d, 2, 2);
    }
}
