using FlowPainter.Application.FlowPainting.Fields;
using FlowPainter.Domain.FlowFields;
using FlowPainter.Domain.Geometry;

namespace FlowPainter.Application.Tests.FlowPainting.Fields;

public sealed class DefaultFlowFieldFactoryTests
{
    [Theory]
    [InlineData(0d, 0d, 1.0915020512344809d)]
    [InlineData(0.25d, 0.75d, 2.8446300518189167d)]
    [InlineData(0.5d, 0.5d, 3.1900335075805017d)]
    [InlineData(1d, 1d, 3.510082016023085d)]
    [InlineData(-0.2d, 1.3d, 3.553468602026456d)]
    public void CoherentNoiseProducesStableGoldenAngles(double x, double y, double expected)
    {
        DefaultFlowFieldFactory factory = new();
        IFlowField field = factory.Create(123_456_789, new FlowFieldSettings());

        double actual = field.SampleAngle(x, y);

        Assert.InRange(Math.Abs(expected - actual), 0d, 1e-12d);
    }

    [Fact]
    public void CoherentNoiseIsRepeatableForEqualInputs()
    {
        DefaultFlowFieldFactory factory = new();
        FlowFieldSettings settings = new(scale: 4d, octaves: 5, persistence: 0.6d, lacunarity: 2.1d);
        IFlowField first = factory.Create(42, settings);
        IFlowField second = factory.Create(42, settings);

        for (int index = 0; index < 20; index++)
        {
            double coordinate = index / 13d;
            Assert.Equal(first.SampleAngle(coordinate, 1d - coordinate), second.SampleAngle(coordinate, 1d - coordinate));
        }
    }

    [Fact]
    public void CoherentNoiseChangesWithSeed()
    {
        DefaultFlowFieldFactory factory = new();
        FlowFieldSettings settings = new();
        IFlowField first = factory.Create(1, settings);
        IFlowField second = factory.Create(2, settings);

        Assert.NotEqual(first.SampleAngle(0.3d, 0.7d), second.SampleAngle(0.3d, 0.7d));
    }

    [Fact]
    public void AngleOffsetRotatesField()
    {
        DefaultFlowFieldFactory factory = new();
        IFlowField baseline = factory.Create(10, new FlowFieldSettings(angleOffsetRadians: 0d));
        IFlowField rotated = factory.Create(10, new FlowFieldSettings(angleOffsetRadians: 0.4d));

        double expected = AngleMath.NormalizeRadians(baseline.SampleAngle(0.2d, 0.8d) + 0.4d);

        Assert.InRange(Math.Abs(expected - rotated.SampleAngle(0.2d, 0.8d)), 0d, 1e-12d);
    }

    [Fact]
    public void LegacyTrigonometricModeMatchesCharacterizedFormula()
    {
        DefaultFlowFieldFactory factory = new();
        FlowFieldSettings settings = new(
            FlowFieldKind.LegacyTrigonometric,
            scale: 1d,
            octaves: 1,
            persistence: 1d,
            lacunarity: 1d);
        IFlowField field = factory.Create(1234, settings);
        double x = 0.25d;
        double y = 0.75d;
        double phaseX = (1234 % 10_007) / 10_007d * AngleMath.Tau;
        double phaseY = (1234 % 7_919) / 7_919d * AngleMath.Tau;
        double normalized = Math.Clamp(
            0.5d
                + (Math.Sin((x * 4.1d) + phaseX) * 0.2d)
                + (Math.Cos((y * 3.7d) + phaseY) * 0.2d)
                + (Math.Sin(((x + y) * 2.3d) + (phaseX * 0.5d)) * 0.1d),
            0d,
            1d);

        Assert.InRange(
            Math.Abs((normalized * AngleMath.Tau) - field.SampleAngle(x, y)),
            0d,
            1e-12d);
    }

    [Theory]
    [InlineData(double.NaN, 0d)]
    [InlineData(0d, double.PositiveInfinity)]
    public void FieldRejectsNonFiniteCoordinates(double x, double y)
    {
        IFlowField field = new DefaultFlowFieldFactory().Create(1, new FlowFieldSettings());

        Assert.Throws<ArgumentOutOfRangeException>(() => field.SampleAngle(x, y));
    }

    [Fact]
    public void FactoryRejectsNegativeSeed()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DefaultFlowFieldFactory().Create(-1, new FlowFieldSettings()));
    }
}
