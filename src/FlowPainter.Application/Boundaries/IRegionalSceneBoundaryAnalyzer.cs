using FlowPainter.Application.Segmentation;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Boundaries;

public interface IRegionalSceneBoundaryAnalyzer
{
    Task<SceneBoundaryAnalysisResult> AnalyzeAsync(
        IRgbaPixelSource source,
        RegionalStructureAnalysisResult regionalAnalysis,
        SceneBoundaryAnalysisSettings settings,
        IProgress<SceneBoundaryAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
