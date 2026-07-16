using FlowPainter.Application.Semantics;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Boundaries;

public interface ISceneBoundaryAnalyzer
{
    Task<SceneBoundaryAnalysisResult> AnalyzeAsync(
        IRgbaPixelSource source,
        SemanticAnalysisResult semanticAnalysis,
        SceneBoundaryAnalysisSettings settings,
        IProgress<SceneBoundaryAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
