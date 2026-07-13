using FlowPainter.Application.Semantics;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Semantics;

namespace FlowPainter.Application.Tests.Semantics;

public sealed class SemanticAnalysisResultTests
{
    private static readonly ImageSize MapSize = new(3, 2);

    [Fact]
    public void ConstructorCopiesRegionsAndTrimsProviderIdentifier()
    {
        List<SemanticRegion> regions =
        [
            new SemanticRegion(
                "subject-1",
                new NormalizedRect(0.1d, 0.2d, 0.5d, 0.8d),
                0.8d,
                0.9d,
                SemanticRegionRole.Subject)
        ];
        DetailMap map = DetailMap.CreateUniform(MapSize, 0.25f);

        SemanticAnalysisResult result = new(
            map,
            map,
            map,
            map,
            map,
            regions,
            " provider ");
        regions.Clear();

        Assert.Single(result.Regions);
        Assert.Equal("provider", result.ProviderId);
    }

    [Fact]
    public void ConstructorRejectsMismatchedMapDimensions()
    {
        DetailMap standard = DetailMap.CreateUniform(MapSize, 0f);
        DetailMap different = DetailMap.CreateUniform(new ImageSize(2, 2), 0f);

        Assert.Throws<ArgumentException>(
            () => new SemanticAnalysisResult(
                standard,
                different,
                standard,
                standard,
                standard));
    }

    [Fact]
    public void ConstructorRejectsDuplicateRegionIdentifiersIgnoringCase()
    {
        DetailMap map = DetailMap.CreateUniform(MapSize, 0f);
        SemanticRegion first = new(
            "subject-1",
            new NormalizedRect(0d, 0d, 0.4d, 0.4d),
            1d,
            1d,
            SemanticRegionRole.Subject);
        SemanticRegion second = new(
            "SUBJECT-1",
            new NormalizedRect(0.5d, 0.5d, 1d, 1d),
            1d,
            1d,
            SemanticRegionRole.Subject);

        SemanticRegion[] duplicateRegions = [first, second];

        Assert.Throws<ArgumentException>(
            () => new SemanticAnalysisResult(map, map, map, map, map, duplicateRegions));
    }

    [Fact]
    public void CreateEmptyBuildsZeroMaps()
    {
        SemanticAnalysisResult result = SemanticAnalysisResult.CreateEmpty(MapSize, "empty-provider");

        Assert.Empty(result.Regions);
        Assert.Equal("empty-provider", result.ProviderId);
        Assert.All(result.ImportanceMap.CopyValues(), value => Assert.Equal(0f, value));
    }
}
