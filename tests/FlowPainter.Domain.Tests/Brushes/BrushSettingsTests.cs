using FlowPainter.Domain.Brushes;

namespace FlowPainter.Domain.Tests.Brushes;

public sealed class BrushSettingsTests
{
    [Fact]
    public void ConstructorUsesSolidRoundDefaults()
    {
        BrushSettings settings = new();

        Assert.Equal(BrushKind.SolidRound, settings.Kind);
        Assert.Equal(BrushSettings.DefaultHardness, settings.Hardness);
        Assert.Equal(BrushSettings.DefaultSizeJitter, settings.SizeJitter);
        Assert.Equal(BrushSettings.DefaultOpacityJitter, settings.OpacityJitter);
        Assert.Equal(BrushSettings.DefaultBristleCount, settings.BristleCount);
        Assert.Equal(BrushSettings.DefaultBristleSpread, settings.BristleSpread);
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(1.01d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidHardness(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BrushSettings(hardness: value));
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(1.01d)]
    [InlineData(double.PositiveInfinity)]
    public void ConstructorRejectsInvalidSizeJitter(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BrushSettings(sizeJitter: value));
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(1.01d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidOpacityJitter(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BrushSettings(opacityJitter: value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65)]
    public void ConstructorRejectsInvalidBristleCount(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BrushSettings(bristleCount: value));
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(1.01d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidBristleSpread(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BrushSettings(bristleSpread: value));
    }

    [Fact]
    public void ConstructorPreservesExplicitValues()
    {
        BrushSettings settings = new(BrushKind.Bristle, 0.4d, 0.2d, 0.3d, 11, 0.9d);

        Assert.Equal(BrushKind.Bristle, settings.Kind);
        Assert.Equal(0.4d, settings.Hardness);
        Assert.Equal(0.2d, settings.SizeJitter);
        Assert.Equal(0.3d, settings.OpacityJitter);
        Assert.Equal(11, settings.BristleCount);
        Assert.Equal(0.9d, settings.BristleSpread);
    }
}
