using FlowPainter.Application.Segmentation;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Tests.Segmentation;

public sealed class RegionAdjacencyGraphBuilderTests
{
    private static readonly uint[] ThreeRegionLabels =
    [
        0, 0, 1,
        0, 2, 1,
    ];

    private static readonly uint[] VerticalSplitLabels =
    [
        0, 0, 1, 1,
        0, 0, 1, 1,
        0, 0, 1, 1,
    ];

    private static readonly uint[] HorizontalSplitLabels =
    [
        0, 0, 0,
        0, 0, 0,
        1, 1, 1,
        1, 1, 1,
    ];

    private static readonly uint[] SteppedBoundaryLabels =
    [
        0, 0, 1,
        0, 1, 1,
        0, 0, 1,
    ];

    private static readonly uint[] TwoPixelLabels = [0, 1];
    private static readonly uint[] SingleRegionLabels = [0, 0, 0, 0];

    private static readonly Rgba32[] ChromaticPixels =
    [
        Rgba32.Opaque(255, 0, 0),
        Rgba32.Opaque(0, 0, 255),
    ];

    private static readonly Rgba32[] TexturedPixels =
    [
        Rgba32.Opaque(128, 128, 128),
        Rgba32.Opaque(128, 128, 128),
        Rgba32.Opaque(0, 0, 0),
        Rgba32.Opaque(255, 255, 255),
        Rgba32.Opaque(128, 128, 128),
        Rgba32.Opaque(128, 128, 128),
        Rgba32.Opaque(255, 255, 255),
        Rgba32.Opaque(0, 0, 0),
    ];

    private static readonly uint[] TexturedSplitLabels =
    [
        0, 0, 1, 1,
        0, 0, 1, 1,
    ];

    [Fact]
    public void BuildCreatesOneEdgePerAdjacentPairWithExactLengths()
    {
        RgbaImage source = CreateUniformImage(3, 2, Rgba32.Opaque(120, 120, 120));
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 3, ThreeRegionLabels);

        RegionAdjacencyGraph graph = BuildGraph(source, labels);

        Assert.Equal(3, graph.Edges.Count);
        Assert.Equal(1, GetEdge(graph, 0, 1).SharedBoundaryLength);
        Assert.Equal(2, GetEdge(graph, 0, 2).SharedBoundaryLength);
        Assert.Equal(1, GetEdge(graph, 1, 2).SharedBoundaryLength);
    }

    [Fact]
    public void BuildMeasuresStraightVerticalBoundary()
    {
        RgbaImage source = CreateVerticalSplitImage(
            4,
            3,
            Rgba32.Opaque(0, 0, 0),
            Rgba32.Opaque(255, 255, 255));
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 2, VerticalSplitLabels);

        RegionAdjacency edge = Assert.Single(BuildGraph(source, labels).Edges);

        Assert.Equal(3, edge.SharedBoundaryLength);
        Assert.True(edge.MeanGradient > 99d);
        Assert.Equal(edge.MeanGradient, edge.MaximumGradient, 12);
        Assert.Equal(1d, edge.Continuity, 12);
        Assert.Equal(Math.PI / 2d, edge.PrevailingTangentRadians, 12);
    }

    [Fact]
    public void BuildMeasuresStraightHorizontalBoundary()
    {
        RgbaImage source = CreateHorizontalSplitImage(
            3,
            4,
            Rgba32.Opaque(30, 30, 30),
            Rgba32.Opaque(220, 220, 220));
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 2, HorizontalSplitLabels);

        RegionAdjacency edge = Assert.Single(BuildGraph(source, labels).Edges);

        Assert.Equal(3, edge.SharedBoundaryLength);
        Assert.Equal(1d, edge.Continuity, 12);
        Assert.Equal(0d, edge.PrevailingTangentRadians, 12);
    }

    [Fact]
    public void BuildReportsLowerContinuityForSteppedBoundary()
    {
        RgbaImage source = CreateVerticalSplitImage(
            3,
            3,
            Rgba32.Opaque(20, 20, 20),
            Rgba32.Opaque(230, 230, 230));
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 2, SteppedBoundaryLabels);

        RegionAdjacency edge = Assert.Single(BuildGraph(source, labels).Edges);

        Assert.Equal(5, edge.SharedBoundaryLength);
        Assert.Equal(0.2d, edge.Continuity, 12);
        Assert.Equal(Math.PI / 2d, edge.PrevailingTangentRadians, 12);
    }

    [Fact]
    public void BuildPreservesChromaticDifferenceBeyondLuminanceDifference()
    {
        RgbaImage source = new(new ImageSize(2, 1), ChromaticPixels);
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 2, TwoPixelLabels);

        RegionAdjacency edge = Assert.Single(BuildGraph(source, labels).Edges);

        Assert.True(edge.ColorDifference > edge.LuminanceDifference);
        Assert.True(edge.MeanGradient > edge.LuminanceDifference);
        Assert.True(edge.BoundaryStrength > RegionBoundaryStrengthModel.ContinuityWeight);
    }

    [Fact]
    public void BuildUsesRegionalTextureDifference()
    {
        RgbaImage source = new(new ImageSize(4, 2), TexturedPixels);
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 2, TexturedSplitLabels);

        RegionAdjacency edge = Assert.Single(BuildGraph(source, labels).Edges);

        Assert.True(edge.TextureDifference > 0d);
    }

    [Fact]
    public void BuildRanksHighContrastBoundaryAboveUniformPartition()
    {
        RegionLabelMap labels = RegionLabelMap.Create(new ImageSize(4, 3), 2, VerticalSplitLabels);
        RgbaImage uniform = CreateUniformImage(4, 3, Rgba32.Opaque(128, 128, 128));
        RgbaImage contrast = CreateVerticalSplitImage(
            4,
            3,
            Rgba32.Opaque(0, 0, 0),
            Rgba32.Opaque(255, 255, 255));

        RegionAdjacency uniformEdge = Assert.Single(BuildGraph(uniform, labels).Edges);
        RegionAdjacency contrastEdge = Assert.Single(BuildGraph(contrast, labels).Edges);

        Assert.Equal(RegionBoundaryStrengthModel.ContinuityWeight, uniformEdge.BoundaryStrength, 12);
        Assert.True(contrastEdge.BoundaryStrength > uniformEdge.BoundaryStrength);
    }

    [Fact]
    public void BuildReturnsEmptyGraphForSingleRegion()
    {
        RgbaImage source = CreateUniformImage(2, 2, Rgba32.Opaque(90, 100, 110));
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 1, SingleRegionLabels);

        RegionAdjacencyGraph graph = BuildGraph(source, labels);

        Assert.Empty(graph.Edges);
        Assert.Equal(0, graph.GetDegree(0));
    }

    [Fact]
    public void BuildIsDeterministic()
    {
        RgbaImage source = CreateVerticalSplitImage(
            4,
            3,
            Rgba32.Opaque(15, 80, 150),
            Rgba32.Opaque(220, 160, 40));
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 2, VerticalSplitLabels);

        RegionAdjacency first = Assert.Single(BuildGraph(source, labels).Edges);
        RegionAdjacency second = Assert.Single(BuildGraph(source, labels).Edges);

        Assert.Equal(first.SharedBoundaryLength, second.SharedBoundaryLength);
        Assert.Equal(first.MeanGradient, second.MeanGradient);
        Assert.Equal(first.ColorDifference, second.ColorDifference);
        Assert.Equal(first.PrevailingTangentRadians, second.PrevailingTangentRadians);
        Assert.Equal(first.BoundaryStrength, second.BoundaryStrength);
    }

    [Fact]
    public void BuildDoesNotMutateSource()
    {
        RgbaImage source = CreateVerticalSplitImage(
            4,
            3,
            Rgba32.Opaque(15, 80, 150),
            Rgba32.Opaque(220, 160, 40));
        Rgba32[] before = source.CopyPixels();
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 2, VerticalSplitLabels);

        _ = BuildGraph(source, labels);

        Assert.Equal(before, source.CopyPixels());
    }

    [Fact]
    public void BuildRejectsDimensionMismatch()
    {
        RgbaImage source = CreateUniformImage(2, 2, Rgba32.Opaque(0, 0, 0));
        RegionLabelMap labels = RegionLabelMap.Create(new ImageSize(2, 1), 2, TwoPixelLabels);
        ImageRegion[] regions = RegionDescriptorCalculator.Calculate(
            CreateUniformImage(2, 1, Rgba32.Opaque(0, 0, 0)),
            labels);

        Assert.Throws<ArgumentException>(() => RegionAdjacencyGraphBuilder.Build(
            source,
            labels,
            regions));
    }

    [Fact]
    public void BuildRejectsIncompleteOrUnorderedRegionList()
    {
        RgbaImage source = CreateVerticalSplitImage(
            4,
            3,
            Rgba32.Opaque(0, 0, 0),
            Rgba32.Opaque(255, 255, 255));
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 2, VerticalSplitLabels);
        ImageRegion[] regions = RegionDescriptorCalculator.Calculate(source, labels);

        Assert.Throws<ArgumentException>(() => RegionAdjacencyGraphBuilder.Build(
            source,
            labels,
            regions.Take(1).ToArray()));
        Assert.Throws<ArgumentException>(() => RegionAdjacencyGraphBuilder.Build(
            source,
            labels,
            regions.Reverse().ToArray()));
    }

    [Fact]
    public void BuildHonorsPreCancelledToken()
    {
        RgbaImage source = CreateVerticalSplitImage(
            4,
            3,
            Rgba32.Opaque(0, 0, 0),
            Rgba32.Opaque(255, 255, 255));
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 2, VerticalSplitLabels);
        ImageRegion[] regions = RegionDescriptorCalculator.Calculate(source, labels);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() => RegionAdjacencyGraphBuilder.Build(
            source,
            labels,
            regions,
            cancellation.Token));
    }

    private static RegionAdjacencyGraph BuildGraph(RgbaImage source, RegionLabelMap labels)
    {
        ImageRegion[] regions = RegionDescriptorCalculator.Calculate(source, labels);
        return RegionAdjacencyGraphBuilder.Build(source, labels, regions);
    }

    private static RegionAdjacency GetEdge(RegionAdjacencyGraph graph, int firstRegionId, int secondRegionId)
    {
        Assert.True(graph.TryGetEdge(firstRegionId, secondRegionId, out RegionAdjacency? edge));
        return Assert.IsType<RegionAdjacency>(edge);
    }

    private static RgbaImage CreateUniformImage(int width, int height, Rgba32 color)
    {
        Rgba32[] pixels = new Rgba32[checked(width * height)];
        Array.Fill(pixels, color);
        return new RgbaImage(new ImageSize(width, height), pixels);
    }

    private static RgbaImage CreateVerticalSplitImage(
        int width,
        int height,
        Rgba32 left,
        Rgba32 right)
    {
        Rgba32[] pixels = new Rgba32[checked(width * height)];
        for (int y = 0; y < height; y++)
        {
            int rowOffset = checked(y * width);
            for (int x = 0; x < width; x++)
            {
                pixels[rowOffset + x] = x < width / 2 ? left : right;
            }
        }

        return new RgbaImage(new ImageSize(width, height), pixels);
    }

    private static RgbaImage CreateHorizontalSplitImage(
        int width,
        int height,
        Rgba32 top,
        Rgba32 bottom)
    {
        Rgba32[] pixels = new Rgba32[checked(width * height)];
        for (int y = 0; y < height; y++)
        {
            int rowOffset = checked(y * width);
            for (int x = 0; x < width; x++)
            {
                pixels[rowOffset + x] = y < height / 2 ? top : bottom;
            }
        }

        return new RgbaImage(new ImageSize(width, height), pixels);
    }
}
