using FlowPainter.Application.Segmentation;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Segmentation;
using FlowPainter.Domain.Semantics;

namespace FlowPainter.Application.Tests.Segmentation;

public sealed class RegionalSemanticCompatibilityAdapterTests
{
    [Fact]
    public void CreatePreservesRegionalMapsForLegacyConsumers()
    {
        RegionalStructureAnalysisResult regional = CreateRegionalAnalysis();

        FlowPainter.Application.Semantics.SemanticAnalysisResult compatibility =
            RegionalSemanticCompatibilityAdapter.Create(regional);

        Assert.Same(regional.StructuralSaliencyMap, compatibility.SaliencyMap);
        Assert.Same(regional.ProtectionMap, compatibility.SubjectMap);
        Assert.Same(regional.BoundaryEvidenceMap, compatibility.SilhouetteMap);
        Assert.Same(regional.FocusMap, compatibility.FocalMap);
        Assert.Same(regional.ImportanceMap, compatibility.ImportanceMap);
        Assert.Equal(RegionalSemanticCompatibilityAdapter.ProviderIdentifier, compatibility.ProviderId);
    }

    [Fact]
    public void CreateMapsGeneralizedRolesToCompatibilityRegions()
    {
        RegionalStructureAnalysisResult regional = CreateRegionalAnalysis(
        [
            new RegionRoleOverride(
                "background",
                new NormalizedRect(0d, 0d, 0.5d, 1d),
                RegionRole.Background),
            new RegionRoleOverride(
                "focus",
                new NormalizedRect(0.5d, 0d, 1d, 1d),
                RegionRole.Focal)
        ]);

        FlowPainter.Application.Semantics.SemanticAnalysisResult compatibility =
            RegionalSemanticCompatibilityAdapter.Create(regional);

        Assert.Equal(2, compatibility.Regions.Count);
        Assert.Equal(SemanticRegionRole.Background, compatibility.Regions[0].Role);
        Assert.Equal(SemanticRegionRole.FocalArea, compatibility.Regions[1].Role);
        Assert.All(compatibility.Regions, region => Assert.Equal(SemanticSubjectKind.Unknown, region.Kind));
    }

    [Fact]
    public void CreateRejectsNullAnalysis()
    {
        Assert.Throws<ArgumentNullException>(() => RegionalSemanticCompatibilityAdapter.Create(null!));
    }

    private static RegionalStructureAnalysisResult CreateRegionalAnalysis(
        RegionRoleOverride[]? roleOverrides = null)
    {
        ImageSize size = new(2, 2);
        DetailMap saliency = DetailMap.CreateUniform(size, 0.1f);
        DetailMap protection = DetailMap.CreateUniform(size, 0.2f);
        DetailMap boundary = DetailMap.CreateUniform(size, 0.3f);
        DetailMap focus = DetailMap.CreateUniform(size, 0.4f);
        DetailMap importance = DetailMap.CreateUniform(size, 0.5f);
        DetailMap background = DetailMap.CreateUniform(size, 0.6f);
        DetailMap ignore = DetailMap.CreateUniform(size, 0.7f);
        return new RegionalStructureAnalysisResult(
            saliency,
            protection,
            boundary,
            focus,
            importance,
            background,
            ignore,
            roleOverrides);
    }
}
