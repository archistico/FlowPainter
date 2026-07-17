using FlowPainter.Domain.Boundaries;

namespace FlowPainter.Application.Boundaries;

public readonly record struct RegionalBoundarySample(
    double DistancePixels,
    double BoundaryStrength,
    double Influence,
    bool IsHardBarrier,
    BoundaryVector Normal,
    BoundaryVector Tangent,
    int FirstRegionId,
    int SecondRegionId)
{
    public bool HasBoundary =>
        FirstRegionId >= 0
        && SecondRegionId >= 0
        && Influence > 0d;

    public bool HasDirection => HasBoundary && Tangent.IsDefined;
}
