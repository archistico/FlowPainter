using FlowPainter.Application.FlowPainting.Fields;
using FlowPainter.Domain.FlowFields;

namespace FlowPainter.Application.Tests.FlowPainting.Fields;

public sealed class FlowFieldSettingsTests
{
    [Fact]
    public void ConstructorUsesProductionDefaults()
    {
        FlowFieldSettings settings = new();

        Assert.Equal(FlowFieldKind.CoherentNoise, settings.Kind);
        Assert.Equal(FlowFieldSettings.DefaultScale, settings.Scale);
        Assert.Equal(FlowFieldSettings.DefaultOctaves, settings.Octaves);
        Assert.Equal(FlowFieldSettings.DefaultPersistence, settings.Persistence);
        Assert.Equal(FlowFieldSettings.DefaultLacunarity, settings.Lacunarity);
        Assert.Equal(0d, settings.AngleOffsetRadians);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ConstructorRejectsInvalidScale(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FlowFieldSettings(scale: value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    public void ConstructorRejectsUnsupportedOctaveCount(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FlowFieldSettings(octaves: value));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-0.1d)]
    [InlineData(1.1d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidPersistence(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FlowFieldSettings(persistence: value));
    }

    [Theory]
    [InlineData(0.9d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ConstructorRejectsInvalidLacunarity(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FlowFieldSettings(lacunarity: value));
    }

    [Fact]
    public void ConstructorRejectsNonFiniteAngleOffset()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new FlowFieldSettings(angleOffsetRadians: double.NaN));
    }

    [Fact]
    public void ConstructorRejectsUnknownKind()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new FlowFieldSettings((FlowFieldKind)99));
    }
}
