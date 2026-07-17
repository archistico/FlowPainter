using FlowPainter.Application.Boundaries;
using FlowPainter.Application.Segmentation;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.Boundaries;

public sealed class RegionalBoundaryGuidanceIntegrationTests
{
    [Fact]
    public void CreateAddsRegionalGuidanceToEmptySceneAnalysis()
    {
        ImageSize size = new(7, 1);
        BoundaryGuidanceField field = BoundaryGuidanceField.Create(
            SceneBoundaryAnalysisResult.CreateEmpty(size),
            RegionalBoundaryTestFactory.CreateVerticalSplit(7, 1, 3, 0.85d),
            new BoundaryPaintingSettings(enabled: true, alignmentRadius: 2));

        BoundaryGuidanceSample boundary = field.SampleNearest(new NormalizedPoint(2.5d / 7d, 0.5d));
        BoundaryGuidanceSample distant = field.SampleNearest(new NormalizedPoint(0.5d / 7d, 0.5d));

        Assert.True(boundary.HasRegionalBoundary);
        Assert.True(boundary.Influence > distant.Influence);
        Assert.True(boundary.Hardness > distant.Hardness);
        Assert.True(Math.Abs(boundary.Tangent.Y) > 0.999d);
        Assert.True(boundary.Normal.X > 0.999d);
    }

    [Fact]
    public void CreatePreservesSoftRegionalTransition()
    {
        ImageSize size = new(13, 1);
        BoundaryGuidanceField field = BoundaryGuidanceField.Create(
            SceneBoundaryAnalysisResult.CreateEmpty(size),
            RegionalBoundaryTestFactory.CreateVerticalSplit(13, 1, 6, 0.45d),
            new BoundaryPaintingSettings(
                enabled: true,
                alignmentRadius: 4,
                hardBoundaryThreshold: 0.7d));

        BoundaryGuidanceSample boundary = field.SampleNearest(new NormalizedPoint(5.5d / 13d, 0.5d));
        BoundaryGuidanceSample near = field.SampleNearest(new NormalizedPoint(4.5d / 13d, 0.5d));
        BoundaryGuidanceSample farther = field.SampleNearest(new NormalizedPoint(3.5d / 13d, 0.5d));

        Assert.False(boundary.IsHardBarrier);
        Assert.True(boundary.Influence > near.Influence);
        Assert.True(near.Influence > farther.Influence);
        Assert.True(farther.Influence > 0d);
    }

    [Fact]
    public void CreateRejectsSegmentationWithDifferentDimensions()
    {
        SceneBoundaryAnalysisResult analysis = SceneBoundaryAnalysisResult.CreateEmpty(new ImageSize(4, 4));
        RegionSegmentationResult segmentation = RegionalBoundaryTestFactory.CreateVerticalSplit(5, 4, 2, 0.8d);

        Assert.Throws<ArgumentException>(() => BoundaryGuidanceField.Create(
            analysis,
            segmentation,
            new BoundaryPaintingSettings(enabled: true)));
    }

    [Fact]
    public void RegionalContourReinforcementRaisesDetailWithoutMutatingSource()
    {
        ImageSize size = new(5, 1);
        BoundaryGuidanceField field = BoundaryGuidanceField.Create(
            SceneBoundaryAnalysisResult.CreateEmpty(size),
            RegionalBoundaryTestFactory.CreateVerticalSplit(5, 1, 2, 0.9d),
            new BoundaryPaintingSettings(enabled: true, alignmentRadius: 0));
        DetailMap source = DetailMap.CreateUniform(size, 0.2f);

        DetailMap reinforced = field.CreateReinforcedDetailMap(source, 0.75d);

        Assert.Equal(0.2f, source[1, 0]);
        Assert.True(reinforced[1, 0] > reinforced[0, 0]);
        Assert.True(reinforced[2, 0] > reinforced[4, 0]);
    }
}
