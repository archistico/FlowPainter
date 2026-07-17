using FlowPainter.Application.Segmentation;
using FlowPainter.Application.Semantics;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Boundaries;

public sealed class RegionalSceneBoundaryAnalyzerAdapter : IRegionalSceneBoundaryAnalyzer
{
    private readonly ISceneBoundaryAnalyzer _inner;

    public RegionalSceneBoundaryAnalyzerAdapter(ISceneBoundaryAnalyzer inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public Task<SceneBoundaryAnalysisResult> AnalyzeAsync(
        IRgbaPixelSource source,
        RegionalStructureAnalysisResult regionalAnalysis,
        SceneBoundaryAnalysisSettings settings,
        IProgress<SceneBoundaryAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(regionalAnalysis);
        ArgumentNullException.ThrowIfNull(settings);

        SemanticAnalysisResult compatibility = RegionalSemanticCompatibilityAdapter.Create(regionalAnalysis);
        return _inner.AnalyzeAsync(
            source,
            compatibility,
            settings,
            progress,
            cancellationToken);
    }
}
