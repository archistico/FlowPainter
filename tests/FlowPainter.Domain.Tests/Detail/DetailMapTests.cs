using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;

namespace FlowPainter.Domain.Tests.Detail;

public sealed class DetailMapTests
{
    [Fact]
    public void ConstructorCopiesInputValues()
    {
        float[] values = [0f, 0.25f, 0.5f, 1f];
        DetailMap map = new(2, 2, values);

        values[0] = 1f;

        Assert.Equal(0f, map[0, 0]);
    }

    [Fact]
    public void SampleNearestMapsNormalizedCornersToGridCorners()
    {
        DetailMap map = new(2, 2, [0.1f, 0.2f, 0.3f, 0.4f]);

        Assert.Equal(0.1f, map.SampleNearest(new NormalizedPoint(0d, 0d)));
        Assert.Equal(0.4f, map.SampleNearest(new NormalizedPoint(1d, 1d)));
    }

    [Fact]
    public void ConstructorRejectsMapAboveSupportedImageLimit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DetailMap(10_001, 1, new float[10_001]));
    }

    [Fact]
    public void ConstructorRejectsValuesOutsideNormalizedRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DetailMap(1, 1, [1.1f]));
    }

    [Fact]
    public void CreateUniformFillsRequestedSize()
    {
        FlowPainter.Domain.Images.ImageSize size = new(3, 2);

        DetailMap map = DetailMap.CreateUniform(size, 0.35f);

        Assert.Equal(size, map.Size);
        Assert.All(map.CopyValues(), value => Assert.Equal(0.35f, value));
    }

    [Theory]
    [InlineData(-0.01f)]
    [InlineData(1.01f)]
    [InlineData(float.NaN)]
    public void CreateUniformRejectsInvalidDetail(float value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => DetailMap.CreateUniform(new FlowPainter.Domain.Images.ImageSize(1, 1), value));
    }
}
