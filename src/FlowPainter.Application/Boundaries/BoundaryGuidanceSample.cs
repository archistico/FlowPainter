using FlowPainter.Domain.Boundaries;

namespace FlowPainter.Application.Boundaries;

public readonly record struct BoundaryGuidanceSample(
    double Influence,
    double Hardness,
    double SubjectBoundary,
    double CornerStrength,
    BoundaryVector Tangent,
    double RegionalBoundaryStrength,
    double RegionalDistancePixels,
    BoundaryVector Normal,
    bool IsHardBarrier)
{
    public bool HasDirection => Tangent.IsDefined && Influence > 0d;

    public bool HasRegionalBoundary =>
        RegionalBoundaryStrength > 0d
        && double.IsFinite(RegionalDistancePixels);
}
