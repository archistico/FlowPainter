using FlowPainter.Application.PrimitiveGeneration;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Primitives;
using FlowPainter.Domain.Randomness;

namespace FlowPainter.Application.Tests.PrimitiveGeneration;

public sealed class PrimitiveFactoryAndMutatorTests
{
    [Fact]
    public void FactoryHonorsAllowedKindSet()
    {
        DefaultPrimitiveCandidateFactory factory = new();
        PrimitiveGenerationSettings settings = new(allowedKinds: PrimitiveKindSet.Ellipse);

        GeometricPrimitive primitive = factory.Create(
            0,
            detailMap: null,
            settings,
            new DeterministicRandom(7UL));

        Assert.Equal(PrimitiveKind.Ellipse, primitive.Kind);
    }

    [Fact]
    public void FactoryUsesSmallerShapesInDetailedAreas()
    {
        float[] values = new float[100];
        Array.Fill(values, 1f);
        DetailMap detailMap = new(10, 10, values);
        PrimitiveGenerationSettings settings = new(
            minimumSize: 0.02d,
            maximumSize: 0.5d,
            detailSizeInfluence: 1d,
            detailPlacementBias: 0d,
            allowedKinds: PrimitiveKindSet.Circle);
        DefaultPrimitiveCandidateFactory factory = new();

        GeometricPrimitive primitive = factory.Create(
            0,
            detailMap,
            settings,
            new DeterministicRandom(10UL));

        Assert.Equal(0.02d, primitive.Width, 12);
        Assert.Equal(primitive.Width, primitive.Height, 12);
    }

    [Fact]
    public void MutatorKeepsGeometryInsideSupportedRanges()
    {
        DefaultPrimitiveMutator mutator = new();
        PrimitiveGenerationSettings settings = new(minimumSize: 0.05d, maximumSize: 0.2d);
        GeometricPrimitive primitive = new DefaultPrimitiveCandidateFactory().Create(
            0,
            DetailMap.CreateUniform(new ImageSize(8, 8), 0.5f),
            settings,
            new DeterministicRandom(2UL));
        DeterministicRandom random = new(3UL);

        for (int index = 0; index < 100; index++)
        {
            primitive = mutator.Mutate(primitive, settings, random);
            Assert.InRange(primitive.Center.X, 0d, 1d);
            Assert.InRange(primitive.Center.Y, 0d, 1d);
            Assert.InRange(primitive.Width, settings.MinimumSize, settings.MaximumSize);
            Assert.InRange(primitive.Height, settings.MinimumSize, settings.MaximumSize);
        }
    }
}
