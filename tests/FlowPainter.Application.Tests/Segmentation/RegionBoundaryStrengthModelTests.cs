using FlowPainter.Application.Segmentation;

namespace FlowPainter.Application.Tests.Segmentation;

public sealed class RegionBoundaryStrengthModelTests
{
    [Fact]
    public void CalculateReturnsZeroWithoutBoundaryEvidence()
    {
        double strength = RegionBoundaryStrengthModel.Calculate(0d, 0d, 0d, 0d, 0d);

        Assert.Equal(0d, strength);
    }

    [Fact]
    public void CalculateTreatsContinuityAsWeakBaselineOnly()
    {
        double strength = RegionBoundaryStrengthModel.Calculate(0d, 0d, 0d, 0d, 1d);

        Assert.Equal(RegionBoundaryStrengthModel.ContinuityWeight, strength, 12);
    }

    [Fact]
    public void CalculateIncreasesWithBoundaryEvidence()
    {
        double weak = RegionBoundaryStrengthModel.Calculate(2d, 3d, 4d, 1d, 0.5d);
        double strong = RegionBoundaryStrengthModel.Calculate(30d, 50d, 70d, 20d, 0.5d);

        Assert.InRange(weak, 0d, 1d);
        Assert.InRange(strong, 0d, 1d);
        Assert.True(strong > weak);
    }

    [Fact]
    public void CalculateRejectsInvalidValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RegionBoundaryStrengthModel.Calculate(
            -1d,
            0d,
            0d,
            0d,
            0d));
        Assert.Throws<ArgumentException>(() => RegionBoundaryStrengthModel.Calculate(
            2d,
            1d,
            0d,
            0d,
            0d));
        Assert.Throws<ArgumentOutOfRangeException>(() => RegionBoundaryStrengthModel.Calculate(
            0d,
            0d,
            0d,
            0d,
            1.1d));
    }
}
