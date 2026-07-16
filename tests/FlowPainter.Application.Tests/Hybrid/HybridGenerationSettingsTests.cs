using FlowPainter.Application.Hybrid;
using FlowPainter.Domain.Hybrid;

namespace FlowPainter.Application.Tests.Hybrid;

public sealed class HybridGenerationSettingsTests
{
    [Fact]
    public void DefaultsProvideCompleteBudgetAndMixedInfluence()
    {
        HybridGenerationSettings settings = new();

        Assert.Equal(1d, settings.PrimitiveBudgetFraction + settings.FlowBudgetFraction + settings.RefinementBudgetFraction, 12);
        Assert.Equal(PrimitiveFlowInfluenceKind.Mixed, settings.InfluenceKind);
        Assert.InRange(settings.InfluenceStrength, 0d, 1d);
        Assert.True(settings.MaximumInfluencesPerSample > 0);
    }

    [Theory]
    [InlineData(0.2d, 0.2d, 0.2d)]
    [InlineData(0.5d, 0.4d, 0.2d)]
    [InlineData(0.8d, 0.1d, 0.05d)]
    public void ConstructorRejectsBudgetsThatDoNotAddToOne(
        double primitive,
        double flow,
        double refinement)
    {
        Assert.Throws<ArgumentException>(() => new HybridGenerationSettings(
            primitiveBudgetFraction: primitive,
            flowBudgetFraction: flow,
            refinementBudgetFraction: refinement));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65)]
    public void ConstructorRejectsInvalidMaximumInfluences(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HybridGenerationSettings(
            maximumInfluencesPerSample: value));
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(1.01d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidInfluenceStrength(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HybridGenerationSettings(
            influenceStrength: value));
    }
}
