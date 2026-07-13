using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Semantics;

public interface ISemanticImportanceAnalyzer
{
    Task<SemanticAnalysisResult> AnalyzeAsync(
        IRgbaPixelSource source,
        SemanticAnalysisSettings settings,
        IProgress<SemanticAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
