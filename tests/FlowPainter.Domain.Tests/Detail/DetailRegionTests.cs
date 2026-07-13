using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;

namespace FlowPainter.Domain.Tests.Detail;

public sealed class DetailRegionTests
{
    [Fact]
    public void ConstructorTrimsIdentifierAndOptionalLabel()
    {
        DetailRegion region = new(
            " face-1 ",
            new NormalizedRect(0.1d, 0.1d, 0.5d, 0.5d),
            0.8d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.IncreaseDetail,
            " face ");

        Assert.Equal("face-1", region.Id);
        Assert.Equal("face", region.Label);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void ConstructorRejectsInvalidStrength(double strength)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DetailRegion(
            "region",
            new NormalizedRect(0.1d, 0.1d, 0.5d, 0.5d),
            strength,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.IncreaseDetail));
    }
}
