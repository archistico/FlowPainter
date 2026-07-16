using FlowPainter.Domain.Boundaries;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;

namespace FlowPainter.Domain.Tests.Boundaries;

public sealed class BoundaryDirectionFieldTests
{
    private static readonly BoundaryVector[] SingleVector =
    [
        new BoundaryVector(1d, 0d)
    ];

    private static readonly BoundaryVector[] TwoByTwoVectors =
    [
        new BoundaryVector(1d, 0d),
        new BoundaryVector(0d, 1d),
        new BoundaryVector(-1d, 0d),
        new BoundaryVector(0d, -1d)
    ];

    [Fact]
    public void ConstructorCopiesInputVectors()
    {
        BoundaryVector[] vectors = (BoundaryVector[])TwoByTwoVectors.Clone();
        BoundaryDirectionField field = new(2, 2, vectors);
        vectors[0] = default;

        Assert.Equal(new BoundaryVector(1d, 0d), field[0, 0]);
    }

    [Fact]
    public void ConstructorRejectsIncorrectVectorCount()
    {
        Assert.Throws<ArgumentException>(() => new BoundaryDirectionField(
            2,
            2,
            SingleVector));
    }

    [Fact]
    public void SampleNearestMapsNormalizedCorners()
    {
        BoundaryDirectionField field = new(2, 2, TwoByTwoVectors);

        Assert.Equal(new BoundaryVector(1d, 0d), field.SampleNearest(new NormalizedPoint(0d, 0d)));
        Assert.Equal(new BoundaryVector(0d, -1d), field.SampleNearest(new NormalizedPoint(1d, 1d)));
    }

    [Fact]
    public void CopyVectorsReturnsIndependentArray()
    {
        BoundaryDirectionField field = new(2, 2, TwoByTwoVectors);

        BoundaryVector[] copy = field.CopyVectors();
        copy[0] = default;

        Assert.True(field[0, 0].IsDefined);
    }

    [Fact]
    public void CreateEmptyBuildsUndefinedField()
    {
        BoundaryDirectionField field = BoundaryDirectionField.CreateEmpty(new ImageSize(3, 2));

        Assert.All(field.CopyVectors(), vector => Assert.False(vector.IsDefined));
    }
}
