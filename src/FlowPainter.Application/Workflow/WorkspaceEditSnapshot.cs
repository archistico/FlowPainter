using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Semantics;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Workflow;

public sealed class WorkspaceEditSnapshot
{
    private readonly IReadOnlyList<DetailRegion> _detailRegions;
    private readonly IReadOnlyList<SemanticCorrectionRegion> _semanticCorrections;
    private readonly IReadOnlyList<RegionRoleOverride> _regionRoleOverrides;

    internal WorkspaceEditSnapshot(
        IEnumerable<DetailRegion> detailRegions,
        IEnumerable<SemanticCorrectionRegion> semanticCorrections,
        IEnumerable<RegionRoleOverride> regionRoleOverrides,
        bool isDirty)
    {
        ArgumentNullException.ThrowIfNull(detailRegions);
        ArgumentNullException.ThrowIfNull(semanticCorrections);
        ArgumentNullException.ThrowIfNull(regionRoleOverrides);
        _detailRegions = Array.AsReadOnly(detailRegions.ToArray());
        _semanticCorrections = Array.AsReadOnly(semanticCorrections.ToArray());
        _regionRoleOverrides = Array.AsReadOnly(regionRoleOverrides.ToArray());
        IsDirty = isDirty;
    }

    public IReadOnlyList<DetailRegion> DetailRegions => _detailRegions;

    public IReadOnlyList<SemanticCorrectionRegion> SemanticCorrections => _semanticCorrections;

    public IReadOnlyList<RegionRoleOverride> RegionRoleOverrides => _regionRoleOverrides;

    public bool IsDirty { get; }
}
