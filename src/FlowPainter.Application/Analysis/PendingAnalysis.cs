namespace FlowPainter.Application.Analysis;

public sealed class PendingAnalysis
{
    internal PendingAnalysis(long generation, AnalysisCacheKey cacheKey, AnalysisResult result)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(generation);
        ArgumentNullException.ThrowIfNull(cacheKey);
        ArgumentNullException.ThrowIfNull(result);

        Generation = generation;
        CacheKey = cacheKey;
        Result = result;
    }

    public long Generation { get; }

    public AnalysisCacheKey CacheKey { get; }

    public AnalysisResult Result { get; }
}
