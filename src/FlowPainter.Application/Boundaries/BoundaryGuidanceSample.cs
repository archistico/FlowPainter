using FlowPainter.Domain.Boundaries;

namespace FlowPainter.Application.Boundaries;

public readonly record struct BoundaryGuidanceSample(
    double Influence,
    double Hardness,
    double SubjectBoundary,
    double CornerStrength,
    BoundaryVector Tangent)
{
    public bool HasDirection => Tangent.IsDefined && Influence > 0d;
}
