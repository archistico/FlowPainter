using FlowPainter.Application.Boundaries;
using FlowPainter.Application.Segmentation;
using FlowPainter.Domain.Geometry;

namespace FlowPainter.Application.Tests.Boundaries;

public sealed class RegionalBoundaryFieldTests
{
    [Fact]
    public void VerticalSplitProducesVerticalTangentAndHorizontalNormal()
    {
        RegionSegmentationResult segmentation = RegionalBoundaryTestFactory.CreateVerticalSplit(
            7,
            3,
            3,
            0.8d);

        RegionalBoundaryField field = RegionalBoundaryField.Create(
            segmentation,
            new RegionalBoundaryFieldSettings(maximumDistancePixels: 4));
        RegionalBoundarySample sample = field.Sample(2, 1);

        Assert.True(sample.HasDirection);
        Assert.Equal(0d, sample.DistancePixels, 12);
        Assert.Equal(0.8d, sample.BoundaryStrength, 6);
        Assert.InRange(Math.Abs(sample.Tangent.X), 0d, 1e-12d);
        Assert.True(Math.Abs(sample.Tangent.Y) > 0.999d);
        Assert.True(sample.Normal.X > 0.999d);
        Assert.InRange(Math.Abs(sample.Normal.Y), 0d, 1e-12d);
    }

    [Fact]
    public void OppositeBoundarySidesReceiveOppositeNormals()
    {
        RegionalBoundaryField field = RegionalBoundaryField.Create(
            RegionalBoundaryTestFactory.CreateVerticalSplit(6, 2, 3, 0.8d),
            new RegionalBoundaryFieldSettings(maximumDistancePixels: 2));

        RegionalBoundarySample left = field.Sample(2, 0);
        RegionalBoundarySample right = field.Sample(3, 0);

        Assert.Equal(left.Normal.X, -right.Normal.X, 12);
        Assert.Equal(left.Normal.Y, -right.Normal.Y, 12);
        Assert.Equal(left.Tangent.X, right.Tangent.X, 12);
        Assert.Equal(left.Tangent.Y, right.Tangent.Y, 12);
    }

    [Fact]
    public void HorizontalSplitProducesHorizontalTangentAndVerticalNormal()
    {
        RegionalBoundaryField field = RegionalBoundaryField.Create(
            RegionalBoundaryTestFactory.CreateHorizontalSplit(5, 6, 3, 0.75d),
            new RegionalBoundaryFieldSettings(maximumDistancePixels: 3));

        RegionalBoundarySample upper = field.Sample(2, 2);
        RegionalBoundarySample lower = field.Sample(2, 3);

        Assert.True(upper.Tangent.X > 0.999d);
        Assert.InRange(Math.Abs(upper.Tangent.Y), 0d, 1e-12d);
        Assert.True(upper.Normal.Y > 0.999d);
        Assert.True(lower.Normal.Y < -0.999d);
    }

    [Fact]
    public void DistanceIncreasesSymmetricallyAwayFromBoundary()
    {
        RegionalBoundaryField field = RegionalBoundaryField.Create(
            RegionalBoundaryTestFactory.CreateVerticalSplit(9, 1, 4, 0.6d),
            new RegionalBoundaryFieldSettings(maximumDistancePixels: 4));

        Assert.Equal(0d, field.Sample(3, 0).DistancePixels, 12);
        Assert.Equal(0d, field.Sample(4, 0).DistancePixels, 12);
        Assert.Equal(1d, field.Sample(2, 0).DistancePixels, 12);
        Assert.Equal(1d, field.Sample(5, 0).DistancePixels, 12);
        Assert.Equal(2d, field.Sample(1, 0).DistancePixels, 12);
        Assert.Equal(2d, field.Sample(6, 0).DistancePixels, 12);
    }

    [Fact]
    public void InfluenceFallsSmoothlyWithDistance()
    {
        RegionalBoundaryField field = RegionalBoundaryField.Create(
            RegionalBoundaryTestFactory.CreateVerticalSplit(11, 1, 5, 0.55d),
            new RegionalBoundaryFieldSettings(maximumDistancePixels: 6));

        double atBoundary = field.Sample(4, 0).Influence;
        double nearBoundary = field.Sample(3, 0).Influence;
        double farther = field.Sample(2, 0).Influence;

        Assert.True(atBoundary > nearBoundary);
        Assert.True(nearBoundary > farther);
        Assert.True(farther > 0d);
    }

    [Fact]
    public void WeakBoundaryUsesBroaderTransitionThanStrongBoundary()
    {
        RegionalBoundaryFieldSettings settings = new(
            maximumDistancePixels: 8,
            hardBarrierThreshold: 0.7d,
            hardTransitionRadiusFactor: 0.35d);
        RegionalBoundaryField weak = RegionalBoundaryField.Create(
            RegionalBoundaryTestFactory.CreateVerticalSplit(15, 1, 7, 0.45d),
            settings);
        RegionalBoundaryField strong = RegionalBoundaryField.Create(
            RegionalBoundaryTestFactory.CreateVerticalSplit(15, 1, 7, 0.95d),
            settings);

        Assert.True(weak.Sample(2, 0).Influence > 0d);
        Assert.Equal(0d, strong.Sample(2, 0).Influence, 12);
        Assert.True(strong.Sample(6, 0).Influence > weak.Sample(6, 0).Influence);
    }

    [Fact]
    public void HardBarrierClassificationUsesConfiguredThreshold()
    {
        RegionalBoundaryFieldSettings settings = new(
            maximumDistancePixels: 3,
            hardBarrierThreshold: 0.7d);
        RegionalBoundaryField weak = RegionalBoundaryField.Create(
            RegionalBoundaryTestFactory.CreateVerticalSplit(5, 1, 2, 0.69d),
            settings);
        RegionalBoundaryField hard = RegionalBoundaryField.Create(
            RegionalBoundaryTestFactory.CreateVerticalSplit(5, 1, 2, 0.70d),
            settings);

        Assert.False(weak.Sample(1, 0).IsHardBarrier);
        Assert.True(hard.Sample(1, 0).IsHardBarrier);
    }

    [Fact]
    public void EquidistantPixelChoosesStrongerBoundary()
    {
        RegionSegmentationResult segmentation = RegionalBoundaryTestFactory.CreateThreeVerticalBands(
            7,
            1,
            2,
            5,
            0.3d,
            0.9d);

        RegionalBoundarySample sample = RegionalBoundaryField.Create(
            segmentation,
            new RegionalBoundaryFieldSettings(maximumDistancePixels: 3)).Sample(3, 0);

        Assert.Equal(0.9d, sample.BoundaryStrength, 6);
        Assert.Equal(1, sample.FirstRegionId);
        Assert.Equal(2, sample.SecondRegionId);
    }

    [Fact]
    public void SingleRegionProducesEmptyField()
    {
        RegionalBoundaryField field = RegionalBoundaryField.Create(
            RegionalBoundaryTestFactory.CreateSingleRegion(4, 3));

        RegionalBoundarySample sample = field.SampleNearest(new NormalizedPoint(0.5d, 0.5d));

        Assert.False(sample.HasBoundary);
        Assert.True(double.IsPositiveInfinity(sample.DistancePixels));
        Assert.Equal(0d, sample.BoundaryStrength, 12);
        Assert.Equal(0d, sample.Influence, 12);
        Assert.Equal(-1, sample.FirstRegionId);
        Assert.Equal(-1, sample.SecondRegionId);
    }

    [Fact]
    public void ZeroDistanceRadiusRetainsOnlyBoundarySeeds()
    {
        RegionalBoundaryField field = RegionalBoundaryField.Create(
            RegionalBoundaryTestFactory.CreateVerticalSplit(7, 1, 3, 0.8d),
            new RegionalBoundaryFieldSettings(maximumDistancePixels: 0));

        Assert.True(field.Sample(2, 0).Influence > 0d);
        Assert.True(field.Sample(3, 0).Influence > 0d);
        Assert.Equal(0d, field.Sample(1, 0).Influence, 12);
        Assert.True(double.IsPositiveInfinity(field.Sample(1, 0).DistancePixels));
    }

    [Fact]
    public void CreationIsDeterministic()
    {
        RegionSegmentationResult segmentation = RegionalBoundaryTestFactory.CreateThreeVerticalBands(
            10,
            3,
            3,
            7,
            0.55d,
            0.82d);
        RegionalBoundaryFieldSettings settings = new(maximumDistancePixels: 5);

        RegionalBoundaryField first = RegionalBoundaryField.Create(segmentation, settings);
        RegionalBoundaryField second = RegionalBoundaryField.Create(segmentation, settings);

        for (int y = 0; y < first.Size.Height; y++)
        {
            for (int x = 0; x < first.Size.Width; x++)
            {
                Assert.Equal(first.Sample(x, y), second.Sample(x, y));
            }
        }
    }

    [Fact]
    public void SampleRejectsCoordinatesOutsideField()
    {
        RegionalBoundaryField field = RegionalBoundaryField.Create(
            RegionalBoundaryTestFactory.CreateSingleRegion(2, 2));

        Assert.Throws<ArgumentOutOfRangeException>(() => field.Sample(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => field.Sample(2, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => field.Sample(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => field.Sample(0, 2));
    }

    [Fact]
    public void CreateHonorsPreCancelledToken()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() => RegionalBoundaryField.Create(
            RegionalBoundaryTestFactory.CreateVerticalSplit(4, 4, 2, 0.8d),
            cancellationToken: cancellation.Token));
    }
}
