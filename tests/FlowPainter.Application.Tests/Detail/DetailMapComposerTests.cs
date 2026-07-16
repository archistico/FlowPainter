using FlowPainter.Application.Detail;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.Detail;

public sealed class DetailMapComposerTests
{
    [Fact]
    public void ApplyRegionsIncreasesOnlySelectedArea()
    {
        DetailMap source = new(2, 2, [0.2f, 0.2f, 0.2f, 0.2f]);
        DetailRegion region = new(
            "manual-focus",
            new NormalizedRect(0d, 0d, 0.5d, 0.5d),
            0.5d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.IncreaseDetail);

        DetailMap result = DetailMapComposer.ApplyRegions(source, [region]);

        Assert.Equal(0.6d, result[0, 0], 5);
        Assert.Equal(0.2d, result[1, 0], 5);
        Assert.Equal(0.2d, result[0, 1], 5);
        Assert.Equal(0.2d, result[1, 1], 5);
    }

    [Fact]
    public void ApplyRegionsReducesSelectedAreaTowardZero()
    {
        DetailMap source = new(1, 1, [0.8f]);
        DetailRegion region = new(
            "background",
            new NormalizedRect(0d, 0d, 1d, 1d),
            0.25d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.ReduceDetail);

        DetailMap result = DetailMapComposer.ApplyRegions(source, [region]);

        Assert.Equal(0.6d, result[0, 0], 5);
    }

    [Fact]
    public void ApplyRegionsDoesNotMutateSourceMap()
    {
        DetailMap source = new(1, 1, [0.4f]);
        DetailRegion region = new(
            "focus",
            new NormalizedRect(0d, 0d, 1d, 1d),
            1d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.IncreaseDetail);

        _ = DetailMapComposer.ApplyRegions(source, [region]);

        Assert.Equal(0.4f, source[0, 0]);
    }

    [Fact]
    public void ApplyRegionsComposesOpposingIntentsWithLatestIntentLast()
    {
        DetailMap source = DetailMap.CreateUniform(new ImageSize(1, 1), 0.2f);
        DetailRegion increase = CreateFullRegion(
            "increase",
            0.5d,
            DetailRegionIntent.IncreaseDetail);
        DetailRegion reduce = CreateFullRegion(
            "reduce",
            0.5d,
            DetailRegionIntent.ReduceDetail);

        DetailMap result = DetailMapComposer.ApplyRegions(source, [increase, reduce]);

        Assert.Equal(0.3d, result[0, 0], 5);
    }

    [Fact]
    public void ApplyRegionsLetsLaterIncreaseOverrideEarlierReduction()
    {
        DetailMap source = DetailMap.CreateUniform(new ImageSize(1, 1), 0.2f);
        DetailRegion reduce = CreateFullRegion(
            "reduce",
            0.5d,
            DetailRegionIntent.ReduceDetail);
        DetailRegion increase = CreateFullRegion(
            "increase",
            0.5d,
            DetailRegionIntent.IncreaseDetail);

        DetailMap result = DetailMapComposer.ApplyRegions(source, [reduce, increase]);

        Assert.Equal(0.55d, result[0, 0], 5);
    }

    [Fact]
    public void ApplyRegionsFeathersInfluenceAcrossInsideAndOutsideBorder()
    {
        DetailMap source = DetailMap.CreateUniform(new ImageSize(100, 100), 0f);
        DetailRegion region = new(
            "focus",
            new NormalizedRect(0.25d, 0.25d, 0.75d, 0.75d),
            1d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.IncreaseDetail);

        DetailMap result = DetailMapComposer.ApplyRegions(
            source,
            [region],
            transitionWidth: 0.1d);

        Assert.Equal(1d, result[49, 49], 6);
        Assert.True(result[25, 50] > result[24, 50]);
        Assert.InRange(Math.Abs(result[25, 50] - result[24, 50]), 0d, 0.02d);
        Assert.True(result[20, 50] > 0f);
        Assert.True(result[20, 50] < result[24, 50]);
        Assert.Equal(0d, result[14, 50], 6);
    }

    [Fact]
    public void ApplyRegionsUsesEuclideanFalloffAroundCorners()
    {
        DetailMap source = DetailMap.CreateUniform(new ImageSize(100, 100), 0f);
        DetailRegion region = new(
            "focus",
            new NormalizedRect(0.25d, 0.25d, 0.75d, 0.75d),
            1d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.IncreaseDetail);

        DetailMap result = DetailMapComposer.ApplyRegions(
            source,
            [region],
            transitionWidth: 0.1d);

        Assert.True(result[18, 18] > 0f);
        Assert.Equal(0d, result[17, 17], 6);
    }

    [Fact]
    public void ApplyRegionsMergesSameIntentByMaximumInfluence()
    {
        DetailMap source = DetailMap.CreateUniform(new ImageSize(1, 1), 0f);
        DetailRegion first = CreateFullRegion(
            "first",
            0.5d,
            DetailRegionIntent.IncreaseDetail);
        DetailRegion second = CreateFullRegion(
            "second",
            0.5d,
            DetailRegionIntent.IncreaseDetail);

        DetailMap result = DetailMapComposer.ApplyRegions(
            source,
            [first, second],
            transitionWidth: 0d);

        Assert.Equal(0.5d, result[0, 0], 6);
    }

    [Fact]
    public void ApplyRegionsWithZeroTransitionPreservesHardRectangle()
    {
        DetailMap source = DetailMap.CreateUniform(new ImageSize(10, 10), 0f);
        DetailRegion region = new(
            "focus",
            new NormalizedRect(0.2d, 0.2d, 0.8d, 0.8d),
            1d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.IncreaseDetail);

        DetailMap result = DetailMapComposer.ApplyRegions(
            source,
            [region],
            transitionWidth: 0d);

        Assert.Equal(0d, result[1, 5], 6);
        Assert.Equal(1d, result[2, 5], 6);
    }

    [Theory]
    [InlineData(-0.001d)]
    [InlineData(0.501d)]
    [InlineData(double.NaN)]
    public void ApplyRegionsRejectsInvalidTransitionWidth(double transitionWidth)
    {
        DetailMap source = DetailMap.CreateUniform(new ImageSize(1, 1), 0f);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => DetailMapComposer.ApplyRegions(
                source,
                [],
                transitionWidth));
    }

    [Fact]
    public void ApplyRegionsHonorsPreCancelledToken()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        DetailRegion region = CreateFullRegion(
            "region",
            1d,
            DetailRegionIntent.IncreaseDetail);

        Assert.Throws<OperationCanceledException>(
            () => DetailMapComposer.ApplyRegions(
                DetailMap.CreateUniform(new ImageSize(2, 2), 0.5f),
                [region],
                cancellation.Token));
    }

    private static DetailRegion CreateFullRegion(
        string id,
        double strength,
        DetailRegionIntent intent)
    {
        return new DetailRegion(
            id,
            new NormalizedRect(0d, 0d, 1d, 1d),
            strength,
            DetailRegionOrigin.Manual,
            intent);
    }
}
