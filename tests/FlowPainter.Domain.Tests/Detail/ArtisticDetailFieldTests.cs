using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;

namespace FlowPainter.Domain.Tests.Detail;

public sealed class ArtisticDetailFieldTests
{
    [Fact]
    public void ConstructorCopiesInputValues()
    {
        float[] values = [-1f, -0.25f, 0.5f, 1f];

        ArtisticDetailField field = new(2, 2, values);
        values[0] = 0f;

        Assert.Equal(-1f, field[0, 0]);
    }

    [Theory]
    [InlineData(-1.01f)]
    [InlineData(1.01f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void ConstructorRejectsValuesOutsideSignedRange(float value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ArtisticDetailField(1, 1, [value]));
    }

    [Fact]
    public void SampleNearestMapsNormalizedCorners()
    {
        ArtisticDetailField field = new(2, 2, [-1f, -0.5f, 0.5f, 1f]);

        Assert.Equal(-1f, field.SampleNearest(new NormalizedPoint(0d, 0d)));
        Assert.Equal(1f, field.SampleNearest(new NormalizedPoint(1d, 1d)));
    }

    [Fact]
    public void CreateUniformFillsEntireField()
    {
        ArtisticDetailField field = ArtisticDetailField.CreateUniform(new ImageSize(3, 2), -0.4f);

        Assert.All(field.CopyValues(), value => Assert.Equal(-0.4f, value));
    }
}
