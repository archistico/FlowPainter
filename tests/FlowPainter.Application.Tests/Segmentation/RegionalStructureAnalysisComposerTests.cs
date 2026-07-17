using FlowPainter.Application.Segmentation;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Tests.Segmentation;

public sealed class RegionalStructureAnalysisComposerTests
{
    [Fact]
    public void ComposeRasterizesSharedBoundaryStrength()
    {
        RegionSegmentationResult segmentation = CreateTwoRegionSegmentation();
        DetailMap structural = DetailMap.CreateUniform(segmentation.Labels.Size, 0.2f);

        RegionalStructureAnalysisResult result = RegionalStructureAnalysisComposer.Compose(
            segmentation,
            structural,
            null,
            0d);

        Assert.Equal(0f, result.BoundaryEvidenceMap[0, 0]);
        Assert.Equal(0.8f, result.BoundaryEvidenceMap[1, 0], 5);
        Assert.Equal(0.8f, result.BoundaryEvidenceMap[2, 0], 5);
        Assert.Equal(0f, result.BoundaryEvidenceMap[3, 0]);
    }

    [Fact]
    public void ComposeUsesRegionalStructureWithoutCreatingAutomaticSubjectLabels()
    {
        RegionSegmentationResult segmentation = CreateTwoRegionSegmentation();
        DetailMap structural = new(4, 2,
        [
            0.1f, 0.1f, 0.9f, 0.9f,
            0.1f, 0.1f, 0.9f, 0.9f
        ]);

        RegionalStructureAnalysisResult result = RegionalStructureAnalysisComposer.Compose(
            segmentation,
            structural,
            null,
            0d);

        Assert.True(result.ImportanceMap[3, 0] > result.ImportanceMap[0, 0]);
        Assert.Empty(result.RoleOverrides);
        Assert.All(result.ProtectionMap.CopyValues(), value => Assert.InRange(value, 0f, 1f));
    }

    [Fact]
    public void SubjectOverridePromotesProtectionAndImportance()
    {
        RegionSegmentationResult segmentation = CreateSingleRegionSegmentation(new ImageSize(8, 8));
        DetailMap structural = DetailMap.CreateUniform(segmentation.Labels.Size, 0.1f);
        RegionRoleOverride roleOverride = new(
            "subject",
            new NormalizedRect(0.25d, 0.25d, 0.75d, 0.75d),
            RegionRole.Subject);

        RegionalStructureAnalysisResult result = RegionalStructureAnalysisComposer.Compose(
            segmentation,
            structural,
            [roleOverride],
            0d);

        Assert.Equal(1f, result.ProtectionMap[3, 3]);
        Assert.True(result.ImportanceMap[3, 3] > result.ImportanceMap[0, 0]);
    }

    [Fact]
    public void BackgroundOverrideSuppressesImportanceAndMarksBackground()
    {
        RegionSegmentationResult segmentation = CreateSingleRegionSegmentation(new ImageSize(8, 8));
        DetailMap structural = DetailMap.CreateUniform(segmentation.Labels.Size, 0.8f);
        RegionRoleOverride roleOverride = new(
            "background",
            new NormalizedRect(0.25d, 0.25d, 0.75d, 0.75d),
            RegionRole.Background);

        RegionalStructureAnalysisResult result = RegionalStructureAnalysisComposer.Compose(
            segmentation,
            structural,
            [roleOverride],
            0d);

        Assert.Equal(1f, result.BackgroundRoleMap[3, 3]);
        Assert.Equal(0f, result.ImportanceMap[3, 3]);
        Assert.True(result.ImportanceMap[0, 0] > 0f);
    }

    [Fact]
    public void FocalOverrideTakesPrecedenceOverOverlappingBackground()
    {
        RegionSegmentationResult segmentation = CreateSingleRegionSegmentation(new ImageSize(6, 6));
        DetailMap structural = DetailMap.CreateUniform(segmentation.Labels.Size, 0.2f);
        NormalizedRect bounds = new(0.2d, 0.2d, 0.8d, 0.8d);

        RegionalStructureAnalysisResult result = RegionalStructureAnalysisComposer.Compose(
            segmentation,
            structural,
            [
                new RegionRoleOverride("background", bounds, RegionRole.Background),
                new RegionRoleOverride("focus", bounds, RegionRole.Focal)
            ],
            0d);

        Assert.Equal(1f, result.FocusMap[2, 2]);
        Assert.Equal(1f, result.ProtectionMap[2, 2]);
        Assert.Equal(1f, result.ImportanceMap[2, 2]);
        Assert.Equal(1f, result.BackgroundRoleMap[2, 2]);
    }

    [Fact]
    public void IgnoreOverrideSuppressesAutomaticBoundaryEvidence()
    {
        RegionSegmentationResult segmentation = CreateTwoRegionSegmentation();
        DetailMap structural = DetailMap.CreateUniform(segmentation.Labels.Size, 0.2f);
        RegionRoleOverride roleOverride = new(
            "ignore",
            new NormalizedRect(0d, 0d, 1d, 1d),
            RegionRole.Ignore);

        RegionalStructureAnalysisResult result = RegionalStructureAnalysisComposer.Compose(
            segmentation,
            structural,
            [roleOverride],
            0d);

        Assert.All(result.BoundaryEvidenceMap.CopyValues(), value => Assert.Equal(0f, value));
        Assert.All(result.IgnoreMap.CopyValues(), value => Assert.Equal(1f, value));
    }

    [Fact]
    public void SoftOverrideCreatesGradualTransition()
    {
        RegionSegmentationResult segmentation = CreateSingleRegionSegmentation(new ImageSize(20, 20));
        DetailMap structural = DetailMap.CreateUniform(segmentation.Labels.Size, 0f);
        RegionRoleOverride roleOverride = new(
            "focus",
            new NormalizedRect(0.35d, 0.35d, 0.65d, 0.65d),
            RegionRole.Focal);

        RegionalStructureAnalysisResult result = RegionalStructureAnalysisComposer.Compose(
            segmentation,
            structural,
            [roleOverride],
            0.15d);

        float centre = result.FocusMap[10, 10];
        float boundary = result.FocusMap[7, 10];
        float outside = result.FocusMap[5, 10];
        Assert.True(centre > boundary);
        Assert.True(boundary > outside);
        Assert.True(outside > 0f);
    }

    [Fact]
    public void ComposeRejectsDuplicateOverrideIdentifiers()
    {
        RegionSegmentationResult segmentation = CreateSingleRegionSegmentation(new ImageSize(4, 4));
        DetailMap structural = DetailMap.CreateUniform(segmentation.Labels.Size, 0f);
        NormalizedRect bounds = new(0d, 0d, 0.5d, 0.5d);

        Assert.Throws<ArgumentException>(() => RegionalStructureAnalysisComposer.Compose(
            segmentation,
            structural,
            [
                new RegionRoleOverride("role", bounds, RegionRole.Subject),
                new RegionRoleOverride("ROLE", bounds, RegionRole.Background)
            ],
            0d));
    }

    [Fact]
    public void ComposeRejectsMismatchedDimensions()
    {
        RegionSegmentationResult segmentation = CreateSingleRegionSegmentation(new ImageSize(4, 4));
        DetailMap structural = DetailMap.CreateUniform(new ImageSize(3, 4), 0f);

        Assert.Throws<ArgumentException>(() => RegionalStructureAnalysisComposer.Compose(
            segmentation,
            structural,
            null,
            0d));
    }

    [Fact]
    public void ComposeIsDeterministic()
    {
        RegionSegmentationResult segmentation = CreateTwoRegionSegmentation();
        DetailMap structural = DetailMap.CreateUniform(segmentation.Labels.Size, 0.4f);
        RegionRoleOverride roleOverride = new(
            "subject",
            new NormalizedRect(0.1d, 0.1d, 0.7d, 0.9d),
            RegionRole.Subject);

        RegionalStructureAnalysisResult first = RegionalStructureAnalysisComposer.Compose(
            segmentation,
            structural,
            [roleOverride],
            0.1d);
        RegionalStructureAnalysisResult second = RegionalStructureAnalysisComposer.Compose(
            segmentation,
            structural,
            [roleOverride],
            0.1d);

        Assert.Equal(first.ImportanceMap.CopyValues(), second.ImportanceMap.CopyValues());
        Assert.Equal(first.BoundaryEvidenceMap.CopyValues(), second.BoundaryEvidenceMap.CopyValues());
    }

    private static RegionSegmentationResult CreateTwoRegionSegmentation()
    {
        ImageSize size = new(4, 2);
        RegionLabelMap labels = RegionLabelMap.Create(size, 2,
        [
            0, 0, 1, 1,
            0, 0, 1, 1
        ]);
        ImageRegion first = new(
            0,
            4,
            0.5d,
            new PixelBounds(0, 0, 2, 2),
            new RegionCentroid(1d, 1d),
            new RegionVisualDescriptors(edgeDensity: 0.1d));
        ImageRegion second = new(
            1,
            4,
            0.5d,
            new PixelBounds(2, 0, 4, 2),
            new RegionCentroid(3d, 1d),
            new RegionVisualDescriptors(edgeDensity: 0.4d));
        RegionAdjacency edge = new(
            0,
            1,
            2,
            meanGradient: 0.8d,
            maximumGradient: 0.8d,
            colorDifference: 40d,
            boundaryStrength: 0.8d);
        SegmentationDiagnostics diagnostics = new(
            2,
            true,
            0d,
            2,
            2,
            regionSizes: RegionSizeDistribution.Create([4, 4]));
        return new RegionSegmentationResult(
            labels,
            [first, second],
            new RegionAdjacencyGraph(2, [edge]),
            RegionHierarchy.CreateIdentity(2),
            diagnostics);
    }

    private static RegionSegmentationResult CreateSingleRegionSegmentation(ImageSize size)
    {
        int pixelCount = checked((int)size.PixelCount);
        RegionLabelMap labels = RegionLabelMap.Create(size, 1, new int[pixelCount]);
        ImageRegion region = new(
            0,
            pixelCount,
            1d,
            new PixelBounds(0, 0, size.Width, size.Height),
            new RegionCentroid(size.Width * 0.5d, size.Height * 0.5d));
        SegmentationDiagnostics diagnostics = new(
            1,
            true,
            0d,
            1,
            1,
            regionSizes: RegionSizeDistribution.Create([pixelCount]));
        return new RegionSegmentationResult(
            labels,
            [region],
            RegionAdjacencyGraph.CreateEmpty(1),
            RegionHierarchy.CreateIdentity(1),
            diagnostics);
    }
}
