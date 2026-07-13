using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Detail;

public interface IDetailMapAnalyzer
{
    Task<DetailMap> AnalyzeAsync(
        IRgbaPixelSource source,
        DetailAnalysisSettings settings,
        IProgress<DetailAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
