using FlowPainter.Domain.Images;
using FlowPainter.Domain.Primitives;
using FlowPainter.Domain.Strokes;

namespace FlowPainter.Domain.Hybrid;

public sealed class HybridPlan
{
    public const string CurrentPlannerVersion = "hybrid-primitive-flow-v1";

    public HybridPlan(
        ulong seed,
        PrimitivePlan primitivePlan,
        StrokePlan flowStrokePlan,
        StrokePlan refinementStrokePlan,
        string plannerVersion = CurrentPlannerVersion)
    {
        ArgumentNullException.ThrowIfNull(primitivePlan);
        ArgumentNullException.ThrowIfNull(flowStrokePlan);
        ArgumentNullException.ThrowIfNull(refinementStrokePlan);
        if (string.IsNullOrWhiteSpace(plannerVersion))
        {
            throw new ArgumentException("A hybrid planner version is required.", nameof(plannerVersion));
        }

        ImageSize sourceSize = primitivePlan.SourceSize;
        if (flowStrokePlan.SourceSize != sourceSize || refinementStrokePlan.SourceSize != sourceSize)
        {
            throw new ArgumentException("All hybrid plan layers must use the same source dimensions.");
        }

        if (flowStrokePlan.BackgroundMode != StrokePlanBackgroundMode.SourceImage
            || refinementStrokePlan.BackgroundMode != StrokePlanBackgroundMode.SourceImage)
        {
            throw new ArgumentException(
                "Hybrid stroke layers must use SourceImage background mode so each pass can receive the previous layer.");
        }

        Seed = seed;
        PrimitivePlan = primitivePlan;
        FlowStrokePlan = flowStrokePlan;
        RefinementStrokePlan = refinementStrokePlan;
        PlannerVersion = plannerVersion.Trim();
    }

    public ulong Seed { get; }

    public ImageSize SourceSize => PrimitivePlan.SourceSize;

    public PrimitivePlan PrimitivePlan { get; }

    public StrokePlan FlowStrokePlan { get; }

    public StrokePlan RefinementStrokePlan { get; }

    public string PlannerVersion { get; }
}
