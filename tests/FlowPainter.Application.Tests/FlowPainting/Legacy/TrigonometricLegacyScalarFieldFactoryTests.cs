using FlowPainter.Application.FlowPainting.Legacy;

namespace FlowPainter.Application.Tests.FlowPainting.Legacy;

public sealed class TrigonometricLegacyScalarFieldFactoryTests
{
    [Fact]
    public void EqualSeedsProduceEqualSamples()
    {
        TrigonometricLegacyScalarFieldFactory factory = new();
        ILegacyScalarField first = factory.Create(42);
        ILegacyScalarField second = factory.Create(42);

        Assert.Equal(first.Sample(0.25d, 0.75d), second.Sample(0.25d, 0.75d));
    }

    [Fact]
    public void DifferentSeedsProduceDifferentSamples()
    {
        TrigonometricLegacyScalarFieldFactory factory = new();

        double first = factory.Create(1).Sample(0.25d, 0.75d);
        double second = factory.Create(2).Sample(0.25d, 0.75d);

        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData(0d, 0d)]
    [InlineData(10d, -4d)]
    [InlineData(0.125d, 0.875d)]
    public void SamplesRemainInNormalizedRange(double x, double y)
    {
        double value = new TrigonometricLegacyScalarFieldFactory().Create(123).Sample(x, y);

        Assert.InRange(value, 0d, 1d);
    }

    [Fact]
    public void CreateRejectsNegativeSeed()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TrigonometricLegacyScalarFieldFactory().Create(-1));
    }
}
