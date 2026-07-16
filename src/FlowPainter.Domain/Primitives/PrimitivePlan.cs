using FlowPainter.Domain.Color;
using FlowPainter.Domain.Images;

namespace FlowPainter.Domain.Primitives;

public sealed class PrimitivePlan
{
    private readonly IReadOnlyList<GeometricPrimitive> _primitives;

    public PrimitivePlan(
        ImageSize sourceSize,
        ulong seed,
        Rgba32 backgroundColor,
        IEnumerable<GeometricPrimitive> primitives,
        string plannerVersion)
    {
        ArgumentNullException.ThrowIfNull(primitives);
        if (string.IsNullOrWhiteSpace(plannerVersion))
        {
            throw new ArgumentException("A planner version is required.", nameof(plannerVersion));
        }

        GeometricPrimitive[] copied = primitives.ToArray();
        for (int index = 0; index < copied.Length; index++)
        {
            if (copied[index].Index != index)
            {
                throw new ArgumentException(
                    "Primitive indexes must be contiguous and match their plan order.",
                    nameof(primitives));
            }
        }

        SourceSize = sourceSize;
        Seed = seed;
        BackgroundColor = backgroundColor;
        PlannerVersion = plannerVersion.Trim();
        _primitives = Array.AsReadOnly(copied);
    }

    public ImageSize SourceSize { get; }

    public ulong Seed { get; }

    public Rgba32 BackgroundColor { get; }

    public string PlannerVersion { get; }

    public IReadOnlyList<GeometricPrimitive> Primitives => _primitives;
}
