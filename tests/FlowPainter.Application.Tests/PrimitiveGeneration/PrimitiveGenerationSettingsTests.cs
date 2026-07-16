using FlowPainter.Application.PrimitiveGeneration;
using FlowPainter.Domain.Primitives;

namespace FlowPainter.Application.Tests.PrimitiveGeneration;

public sealed class PrimitiveGenerationSettingsTests
{
    [Fact]
    public void ConstructorExposesValidatedValues()
    {
        PrimitiveGenerationSettings settings = new(
            primitiveCount: 50,
            candidatesPerStep: 8,
            mutationIterations: 4,
            minimumSize: 0.03d,
            maximumSize: 0.4d,
            opacity: 0.6d,
            detailSizeInfluence: 0.5d,
            detailPlacementBias: 3d,
            detailErrorWeight: 2d,
            allowedKinds: PrimitiveKindSet.Triangle | PrimitiveKindSet.Circle);

        Assert.Equal(50, settings.PrimitiveCount);
        Assert.Equal(8, settings.CandidatesPerStep);
        Assert.Equal(PrimitiveKindSet.Triangle | PrimitiveKindSet.Circle, settings.AllowedKinds);
        Assert.Equal(0.6d, settings.Opacity, 12);
    }

    [Fact]
    public void ConstructorRejectsEmptyKindSet()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PrimitiveGenerationSettings(
            allowedKinds: PrimitiveKindSet.None));
    }

    [Fact]
    public void ConstructorRejectsMaximumSizeBelowMinimum()
    {
        Assert.Throws<ArgumentException>(() => new PrimitiveGenerationSettings(
            minimumSize: 0.4d,
            maximumSize: 0.2d));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-0.1d)]
    [InlineData(1.1d)]
    public void ConstructorRejectsInvalidOpacity(double opacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PrimitiveGenerationSettings(opacity: opacity));
    }
}
