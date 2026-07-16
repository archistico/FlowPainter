using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Semantics;

namespace FlowPainter.Application.Workflow;

public sealed class WorkspaceEditSnapshot
{
    private readonly IReadOnlyList<DetailRegion> _detailRegions;
    private readonly IReadOnlyList<SemanticCorrectionRegion> _semanticCorrections;

    internal WorkspaceEditSnapshot(
        IEnumerable<DetailRegion> detailRegions,
        IEnumerable<SemanticCorrectionRegion> semanticCorrections,
        bool isDirty)
    {
        ArgumentNullException.ThrowIfNull(detailRegions);
        ArgumentNullException.ThrowIfNull(semanticCorrections);
        _detailRegions = Array.AsReadOnly(detailRegions.ToArray());
        _semanticCorrections = Array.AsReadOnly(semanticCorrections.ToArray());
        IsDirty = isDirty;
    }

    public IReadOnlyList<DetailRegion> DetailRegions => _detailRegions;

    public IReadOnlyList<SemanticCorrectionRegion> SemanticCorrections => _semanticCorrections;

    public bool IsDirty { get; }
}
