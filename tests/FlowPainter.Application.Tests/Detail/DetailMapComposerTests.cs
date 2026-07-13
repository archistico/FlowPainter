using FlowPainter.Application.Detail;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;

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
    public void ApplyRegionsComposesOverlappingIntentInOrder()
    {
        DetailMap source = DetailMap.CreateUniform(new FlowPainter.Domain.Images.ImageSize(1, 1), 0.2f);
        DetailRegion increase = new(
            "increase",
            new NormalizedRect(0d, 0d, 1d, 1d),
            0.5d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.IncreaseDetail);
        DetailRegion reduce = new(
            "reduce",
            new NormalizedRect(0d, 0d, 1d, 1d),
            0.5d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.ReduceDetail);

        DetailMap result = DetailMapComposer.ApplyRegions(source, [increase, reduce]);

        Assert.Equal(0.3d, result[0, 0], 5);
    }

    [Fact]
    public void ApplyRegionsHonorsPreCancelledToken()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        DetailRegion region = new(
            "region",
            new NormalizedRect(0d, 0d, 1d, 1d),
            1d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.IncreaseDetail);

        Assert.Throws<OperationCanceledException>(
            () => DetailMapComposer.ApplyRegions(
                DetailMap.CreateUniform(new FlowPainter.Domain.Images.ImageSize(2, 2), 0.5f),
                [region],
                cancellation.Token));
    }
}
