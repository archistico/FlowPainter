using FlowPainter.Application.Segmentation;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Tests.Segmentation;

public sealed class SegmentationDiagnosticRendererTests
{
    private static readonly uint[] SplitLabels = [0, 0, 1, 1];
    private static readonly uint[] SingleRegionLabels = [0, 0];
    private static readonly int[] IdentityParents = [0, 1];
    private static readonly int[] MergedParents = [0, 0];
    private static readonly RegionHierarchyLevel[] MergedHierarchyLevels =
    [
        new RegionHierarchyLevel(0, 2, IdentityParents),
        new RegionHierarchyLevel(1, 1, MergedParents)
    ];
    private static readonly RegionAdjacency[] StrongAdjacencies =
    [
        new RegionAdjacency(0, 1, 1, boundaryStrength: 0.8d)
    ];
    private static readonly Rgba32[] MeanColorSourcePixels =
    [
        Rgba32.Opaque(10, 20, 30),
        Rgba32.Opaque(30, 40, 50),
        Rgba32.Opaque(100, 110, 120),
        Rgba32.Opaque(140, 150, 160),
    ];

    [Fact]
    public void CreateMeanColorPreviewUsesRegionalChannelMeans()
    {
        RgbaImage source = new(new ImageSize(4, 1), MeanColorSourcePixels);
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 2, SplitLabels);

        RgbaImage preview = SegmentationDiagnosticRenderer.CreateMeanColorPreview(source, labels);

        Assert.Equal(Rgba32.Opaque(20, 30, 40), preview[0, 0]);
        Assert.Equal(Rgba32.Opaque(20, 30, 40), preview[1, 0]);
        Assert.Equal(Rgba32.Opaque(120, 130, 140), preview[2, 0]);
        Assert.Equal(Rgba32.Opaque(120, 130, 140), preview[3, 0]);
    }

    [Fact]
    public void CreateBoundaryOverlayMarksBothSidesOfBoundary()
    {
        RgbaImage source = CreateUniformImage(4, 1);
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 2, SplitLabels);
        Rgba32 boundary = Rgba32.Opaque(1, 2, 3);

        RgbaImage overlay = SegmentationDiagnosticRenderer.CreateBoundaryOverlay(source, labels, boundary);

        Assert.Equal(source[0, 0], overlay[0, 0]);
        Assert.Equal(boundary, overlay[1, 0]);
        Assert.Equal(boundary, overlay[2, 0]);
        Assert.Equal(source[3, 0], overlay[3, 0]);
    }

    [Fact]
    public void DiagnosticImagesMatchLabelDimensions()
    {
        RgbaImage source = CreateUniformImage(4, 1);
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 2, SplitLabels);

        Assert.Equal(source.Size, SegmentationDiagnosticRenderer.CreateMeanColorPreview(source, labels).Size);
        Assert.Equal(source.Size, SegmentationDiagnosticRenderer.CreateBoundaryOverlay(source, labels).Size);
    }

    [Fact]
    public void DiagnosticRenderingDoesNotMutateSource()
    {
        RgbaImage source = CreateUniformImage(4, 1);
        Rgba32[] before = source.CopyPixels();
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 2, SplitLabels);

        _ = SegmentationDiagnosticRenderer.CreateMeanColorPreview(source, labels);
        _ = SegmentationDiagnosticRenderer.CreateBoundaryOverlay(source, labels);

        Assert.Equal(before, source.CopyPixels());
    }

    [Fact]
    public void DiagnosticRenderingRejectsDimensionMismatch()
    {
        RgbaImage source = CreateUniformImage(4, 1);
        RegionLabelMap labels = RegionLabelMap.Create(new ImageSize(2, 1), 1, SingleRegionLabels);

        Assert.Throws<ArgumentException>(() => SegmentationDiagnosticRenderer.CreateMeanColorPreview(source, labels));
        Assert.Throws<ArgumentException>(() => SegmentationDiagnosticRenderer.CreateBoundaryOverlay(source, labels));
    }

    [Fact]
    public void DiagnosticRenderingIsDeterministic()
    {
        RgbaImage source = CreateUniformImage(4, 1);
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 2, SplitLabels);

        RgbaImage first = SegmentationDiagnosticRenderer.CreateBoundaryOverlay(source, labels);
        RgbaImage second = SegmentationDiagnosticRenderer.CreateBoundaryOverlay(source, labels);

        Assert.Equal(first.CopyPixels(), second.CopyPixels());
    }

    [Fact]
    public void DiagnosticRenderingHonorsPreCancelledToken()
    {
        RgbaImage source = CreateUniformImage(4, 1);
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 2, SplitLabels);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() => SegmentationDiagnosticRenderer.CreateMeanColorPreview(
            source,
            labels,
            cancellation.Token));
        Assert.ThrowsAny<OperationCanceledException>(() => SegmentationDiagnosticRenderer.CreateBoundaryOverlay(
            source,
            labels,
            cancellationToken: cancellation.Token));
    }

    [Fact]
    public void CreateHierarchyMeanColorPreviewUsesSelectedParentLevel()
    {
        RgbaImage source = new(new ImageSize(4, 1), MeanColorSourcePixels);
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 2, SplitLabels);
        RegionHierarchy hierarchy = new(2, MergedHierarchyLevels);

        RgbaImage preview = SegmentationDiagnosticRenderer.CreateHierarchyMeanColorPreview(
            source,
            labels,
            hierarchy,
            1);

        Assert.All(preview.CopyPixels(), pixel => Assert.Equal(Rgba32.Opaque(70, 80, 90), pixel));
    }

    [Fact]
    public void CreateStrongBoundaryOverlayOnlyMarksEdgesAboveThreshold()
    {
        RgbaImage source = CreateUniformImage(4, 1);
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 2, SplitLabels);
        RegionAdjacencyGraph adjacency = new(2, StrongAdjacencies);
        Rgba32 boundary = Rgba32.Opaque(5, 6, 7);

        RgbaImage visible = SegmentationDiagnosticRenderer.CreateStrongBoundaryOverlay(
            source,
            labels,
            adjacency,
            0.7d,
            boundary);
        RgbaImage hidden = SegmentationDiagnosticRenderer.CreateStrongBoundaryOverlay(
            source,
            labels,
            adjacency,
            0.9d,
            boundary);

        Assert.Equal(boundary, visible[1, 0]);
        Assert.Equal(boundary, visible[2, 0]);
        Assert.Equal(source.CopyPixels(), hidden.CopyPixels());
    }

    [Fact]
    public void CreateHierarchyMeanColorPreviewRejectsMismatchedHierarchy()
    {
        RgbaImage source = CreateUniformImage(4, 1);
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 2, SplitLabels);
        RegionHierarchy hierarchy = RegionHierarchy.CreateIdentity(1);

        Assert.Throws<ArgumentException>(() => SegmentationDiagnosticRenderer.CreateHierarchyMeanColorPreview(
            source,
            labels,
            hierarchy,
            0));
    }

    [Fact]
    public void CreateStrongBoundaryOverlayRejectsMismatchedAdjacencyGraph()
    {
        RgbaImage source = CreateUniformImage(4, 1);
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 2, SplitLabels);
        RegionAdjacencyGraph adjacency = RegionAdjacencyGraph.CreateEmpty(1);

        Assert.Throws<ArgumentException>(() => SegmentationDiagnosticRenderer.CreateStrongBoundaryOverlay(
            source,
            labels,
            adjacency,
            0.5d));
    }

    private static RgbaImage CreateUniformImage(int width, int height)
    {
        Rgba32[] pixels = new Rgba32[checked(width * height)];
        Array.Fill(pixels, Rgba32.Opaque(90, 100, 110));
        return new RgbaImage(new ImageSize(width, height), pixels);
    }
}
