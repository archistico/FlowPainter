using FlowPainter.Application.Semantics;
using FlowPainter.Domain.Segmentation;
using FlowPainter.Domain.Semantics;

namespace FlowPainter.Application.Segmentation;

public static class RegionalSemanticCompatibilityAdapter
{
    public const string ProviderIdentifier = "slic-regional-compatibility-v1";

    public static SemanticAnalysisResult Create(RegionalStructureAnalysisResult regionalAnalysis)
    {
        ArgumentNullException.ThrowIfNull(regionalAnalysis);
        SemanticRegion[] regions = regionalAnalysis.RoleOverrides
            .Select(CreateCompatibilityRegion)
            .ToArray();

        return new SemanticAnalysisResult(
            regionalAnalysis.StructuralSaliencyMap,
            regionalAnalysis.ProtectionMap,
            regionalAnalysis.BoundaryEvidenceMap,
            regionalAnalysis.FocusMap,
            regionalAnalysis.ImportanceMap,
            regions,
            ProviderIdentifier);
    }

    private static SemanticRegion CreateCompatibilityRegion(RegionRoleOverride roleOverride)
    {
        SemanticRegionRole role = roleOverride.Role switch
        {
            RegionRole.Background => SemanticRegionRole.Background,
            RegionRole.Supporting => SemanticRegionRole.SupportingArea,
            RegionRole.Subject => SemanticRegionRole.Subject,
            RegionRole.Focal => SemanticRegionRole.FocalArea,
            RegionRole.CriticalDetail => SemanticRegionRole.CriticalDetail,
            RegionRole.Ignore => SemanticRegionRole.Ignore,
            _ => throw new ArgumentOutOfRangeException(
                nameof(roleOverride),
                roleOverride.Role,
                "Unknown region role.")
        };

        double importance = roleOverride.Role switch
        {
            RegionRole.Background => 0d,
            RegionRole.Supporting => 0.45d,
            RegionRole.Subject => 0.85d,
            RegionRole.Focal => 0.95d,
            RegionRole.CriticalDetail => 1d,
            RegionRole.Ignore => 0d,
            _ => 0d
        };

        return new SemanticRegion(
            roleOverride.Id,
            roleOverride.Bounds,
            1d,
            importance,
            role,
            SemanticSubjectKind.Unknown,
            roleOverride.Label,
            ProviderIdentifier);
    }
}
