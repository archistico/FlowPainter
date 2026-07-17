using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Domain.Tests.Segmentation;

public sealed class RegionRoleOverrideTests
{
    [Fact]
    public void ConstructorTrimsOptionalText()
    {
        RegionRoleOverride roleOverride = new(
            "  role-1  ",
            new NormalizedRect(0.1d, 0.2d, 0.4d, 0.6d),
            RegionRole.Focal,
            "  Face  ",
            "  legacy-region  ");

        Assert.Equal("role-1", roleOverride.Id);
        Assert.Equal("Face", roleOverride.Label);
        Assert.Equal("legacy-region", roleOverride.SourceRegionId);
    }

    [Fact]
    public void ConstructorRejectsBlankIdentifier()
    {
        Assert.Throws<ArgumentException>(() => new RegionRoleOverride(
            " ",
            new NormalizedRect(0d, 0d, 0.5d, 0.5d),
            RegionRole.Subject));
    }

    [Fact]
    public void ConstructorRejectsUnknownRole()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionRoleOverride(
            "role-1",
            new NormalizedRect(0d, 0d, 0.5d, 0.5d),
            (RegionRole)99));
    }

    [Fact]
    public void BlankOptionalTextBecomesNull()
    {
        RegionRoleOverride roleOverride = new(
            "role-1",
            new NormalizedRect(0d, 0d, 0.5d, 0.5d),
            RegionRole.Background,
            " ",
            " ");

        Assert.Null(roleOverride.Label);
        Assert.Null(roleOverride.SourceRegionId);
    }
}
