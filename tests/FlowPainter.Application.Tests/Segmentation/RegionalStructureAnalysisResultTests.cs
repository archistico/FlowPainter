using FlowPainter.Application.Segmentation;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Tests.Segmentation;

public sealed class RegionalStructureAnalysisResultTests
{
    [Fact]
    public void ConstructorAcceptsMatchingMapsAndCopiesOverrides()
    {
        ImageSize size = new(3, 2);
        DetailMap map = DetailMap.CreateUniform(size, 0.25f);
        List<RegionRoleOverride> overrides =
        [
            new RegionRoleOverride(
                "role-1",
                new NormalizedRect(0d, 0d, 0.5d, 0.5d),
                RegionRole.Subject)
        ];

        RegionalStructureAnalysisResult result = new(
            map,
            map,
            map,
            map,
            map,
            map,
            map,
            overrides,
            "test-regional");
        overrides.Clear();

        Assert.Single(result.RoleOverrides);
        Assert.Equal("test-regional", result.ProviderId);
        Assert.Equal(size, result.ImportanceMap.Size);
    }

    [Fact]
    public void ConstructorRejectsMismatchedMapSizes()
    {
        DetailMap small = DetailMap.CreateUniform(new ImageSize(2, 2), 0f);
        DetailMap large = DetailMap.CreateUniform(new ImageSize(3, 2), 0f);

        Assert.Throws<ArgumentException>(() => new RegionalStructureAnalysisResult(
            small,
            small,
            small,
            small,
            large,
            small,
            small));
    }

    [Fact]
    public void ConstructorRejectsDuplicateOverrideIdentifiers()
    {
        DetailMap map = DetailMap.CreateUniform(new ImageSize(2, 2), 0f);
        RegionRoleOverride first = new(
            "role-1",
            new NormalizedRect(0d, 0d, 0.5d, 0.5d),
            RegionRole.Subject);
        RegionRoleOverride second = new(
            "ROLE-1",
            new NormalizedRect(0.5d, 0.5d, 1d, 1d),
            RegionRole.Background);

        Assert.Throws<ArgumentException>(() => new RegionalStructureAnalysisResult(
            map,
            map,
            map,
            map,
            map,
            map,
            map,
            [first, second]));
    }

    [Fact]
    public void CreateEmptyProducesZeroMaps()
    {
        ImageSize size = new(2, 3);

        RegionalStructureAnalysisResult result = RegionalStructureAnalysisResult.CreateEmpty(size);

        Assert.All(result.ImportanceMap.CopyValues(), value => Assert.Equal(0f, value));
        Assert.Empty(result.RoleOverrides);
    }
}
