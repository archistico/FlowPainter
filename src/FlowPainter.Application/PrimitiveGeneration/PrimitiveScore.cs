using FlowPainter.Domain.Primitives;

namespace FlowPainter.Application.PrimitiveGeneration;

public sealed class PrimitiveScore
{
    public PrimitiveScore(
        GeometricPrimitive primitive,
        PrimitiveRasterMask mask,
        double improvement)
    {
        ArgumentNullException.ThrowIfNull(primitive);
        ArgumentNullException.ThrowIfNull(mask);
        if (!double.IsFinite(improvement))
        {
            throw new ArgumentOutOfRangeException(
                nameof(improvement),
                improvement,
                "Primitive score improvement must be finite.");
        }

        Primitive = primitive;
        Mask = mask;
        Improvement = improvement;
    }

    public GeometricPrimitive Primitive { get; }

    public PrimitiveRasterMask Mask { get; }

    public double Improvement { get; }
}
