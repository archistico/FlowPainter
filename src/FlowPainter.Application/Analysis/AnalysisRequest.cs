using FlowPainter.Application.Background;
using FlowPainter.Application.Boundaries;
using FlowPainter.Application.Detail;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.Segmentation;
using FlowPainter.Application.Semantics;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Semantics;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Analysis;

public sealed class AnalysisRequest
{
    private readonly IReadOnlyList<DetailRegion> _detailRegions;
    private readonly IReadOnlyList<SemanticCorrectionRegion> _semanticCorrections;
    private readonly IReadOnlyList<RegionRoleOverride> _regionRoleOverrides;

    public AnalysisRequest(
        IRgbaPixelSource source,
        Guid sourceIdentity,
        DetailAnalysisSettings detailSettings,
        DetailInfluenceSettings detailInfluenceSettings,
        SemanticAnalysisSettings semanticSettings,
        SceneBoundaryAnalysisSettings boundarySettings,
        BackgroundSuppressionSettings backgroundSettings,
        IReadOnlyList<DetailRegion>? detailRegions,
        IReadOnlyList<SemanticCorrectionRegion>? semanticCorrections,
        long detailRegionRevision,
        long semanticCorrectionRevision,
        RegionSegmentationSettings? segmentationSettings = null,
        RegionMergeSettings? mergeSettings = null,
        IReadOnlyList<RegionRoleOverride>? regionRoleOverrides = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(detailSettings);
        ArgumentNullException.ThrowIfNull(detailInfluenceSettings);
        ArgumentNullException.ThrowIfNull(semanticSettings);
        ArgumentNullException.ThrowIfNull(boundarySettings);
        ArgumentNullException.ThrowIfNull(backgroundSettings);

        Source = source;
        DetailSettings = detailSettings;
        DetailInfluenceSettings = detailInfluenceSettings;
        SemanticSettings = semanticSettings;
        SegmentationSettings = segmentationSettings ?? new RegionSegmentationSettings();
        MergeSettings = mergeSettings ?? new RegionMergeSettings();
        BoundarySettings = boundarySettings;
        BackgroundSettings = backgroundSettings;
        _detailRegions = Array.AsReadOnly(detailRegions?.ToArray() ?? []);
        _semanticCorrections = Array.AsReadOnly(semanticCorrections?.ToArray() ?? []);
        _regionRoleOverrides = Array.AsReadOnly(regionRoleOverrides?.ToArray() ?? []);
        CacheKey = AnalysisCacheKey.Create(
            sourceIdentity,
            source.Size,
            detailSettings,
            detailInfluenceSettings,
            semanticSettings,
            boundarySettings,
            backgroundSettings,
            detailRegionRevision,
            semanticCorrectionRevision,
            SegmentationSettings,
            MergeSettings);
    }

    public IRgbaPixelSource Source { get; }

    public DetailAnalysisSettings DetailSettings { get; }

    public DetailInfluenceSettings DetailInfluenceSettings { get; }

    public SemanticAnalysisSettings SemanticSettings { get; }

    public RegionSegmentationSettings SegmentationSettings { get; }

    public RegionMergeSettings MergeSettings { get; }

    public SceneBoundaryAnalysisSettings BoundarySettings { get; }

    public BackgroundSuppressionSettings BackgroundSettings { get; }

    public IReadOnlyList<DetailRegion> DetailRegions => _detailRegions;

    public IReadOnlyList<SemanticCorrectionRegion> SemanticCorrections => _semanticCorrections;

    public IReadOnlyList<RegionRoleOverride> RegionRoleOverrides => _regionRoleOverrides;

    public AnalysisCacheKey CacheKey { get; }
}
