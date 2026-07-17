using FlowPainter.Application.Segmentation;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.Segmentation;

public sealed class RegionalDetailMapComposerTests
{
    [Fact]
    public void CombinePromotesStructuralDetailWithFixedRegionalInfluence()
    {
        ImageSize size = new(2, 1);
        DetailMap structural = new(2, 1, [0.2f, 0.8f]);
        RegionalStructureAnalysisResult regional = CreateRegional(size, new DetailMap(2, 1, [1f, 0f]));

        DetailMap result = RegionalDetailMapComposer.Combine(structural, regional);

        Assert.True(result[0, 0] > structural[0, 0]);
        Assert.Equal(structural[1, 0], result[1, 0]);
    }

    [Fact]
    public void CombineDoesNotMutateInputMaps()
    {
        ImageSize size = new(2, 1);
        DetailMap structural = new(2, 1, [0.25f, 0.5f]);
        RegionalStructureAnalysisResult regional = CreateRegional(size, new DetailMap(2, 1, [0.5f, 0.5f]));
        float[] before = structural.CopyValues();

        _ = RegionalDetailMapComposer.Combine(structural, regional);

        Assert.Equal(before, structural.CopyValues());
    }

    [Fact]
    public void CombineRejectsMismatchedDimensions()
    {
        DetailMap structural = DetailMap.CreateUniform(new ImageSize(2, 2), 0f);
        RegionalStructureAnalysisResult regional = CreateRegional(
            new ImageSize(3, 2),
            DetailMap.CreateUniform(new ImageSize(3, 2), 0f));

        Assert.Throws<ArgumentException>(() => RegionalDetailMapComposer.Combine(structural, regional));
    }

    private static RegionalStructureAnalysisResult CreateRegional(
        ImageSize size,
        DetailMap importance)
    {
        DetailMap empty = DetailMap.CreateUniform(size, 0f);
        return new RegionalStructureAnalysisResult(
            empty,
            empty,
            empty,
            empty,
            importance,
            empty,
            empty);
    }
}
