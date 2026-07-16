using FlowPainter.Domain.Generation;

namespace FlowPainter.Application.Workloads;

public readonly record struct GenerationWorkEstimate(
    GenerativeMode Mode,
    long FlowSegmentSteps,
    long PrimitiveScoreAttempts,
    long PrimitivePixelEvaluations)
{
    public bool IncludesFlow => FlowSegmentSteps > 0;

    public bool IncludesPrimitives => PrimitiveScoreAttempts > 0;
}
