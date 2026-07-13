using FlowPainter.Domain.Images;

namespace FlowPainter.Domain.Strokes;

public sealed class StrokePlan
{
    public const string LegacyPlannerVersion = "legacy-flow-v1";

    private readonly IReadOnlyList<FlowStroke> _strokes;

    public StrokePlan(
        ImageSize sourceSize,
        ulong seed,
        int fieldSeed,
        int referenceMaximumDimension,
        IEnumerable<FlowStroke> strokes,
        StrokePlanBackgroundMode backgroundMode = StrokePlanBackgroundMode.SourceImage,
        string plannerVersion = LegacyPlannerVersion)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fieldSeed);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(referenceMaximumDimension);
        ArgumentNullException.ThrowIfNull(strokes);

        if (!Enum.IsDefined(backgroundMode))
        {
            throw new ArgumentOutOfRangeException(nameof(backgroundMode), backgroundMode, "Unknown background mode.");
        }

        if (referenceMaximumDimension > ImageSize.MaximumDimension)
        {
            throw new ArgumentOutOfRangeException(
                nameof(referenceMaximumDimension),
                referenceMaximumDimension,
                $"The reference dimension cannot exceed {ImageSize.MaximumDimension:N0} pixels.");
        }

        if (string.IsNullOrWhiteSpace(plannerVersion))
        {
            throw new ArgumentException("A planner version is required.", nameof(plannerVersion));
        }

        FlowStroke[] copiedStrokes = strokes.ToArray();
        for (int index = 0; index < copiedStrokes.Length; index++)
        {
            if (copiedStrokes[index].Index != index)
            {
                throw new ArgumentException(
                    "Stroke indices must be contiguous and match their position in the plan.",
                    nameof(strokes));
            }
        }

        SourceSize = sourceSize;
        Seed = seed;
        FieldSeed = fieldSeed;
        ReferenceMaximumDimension = referenceMaximumDimension;
        BackgroundMode = backgroundMode;
        PlannerVersion = plannerVersion.Trim();
        _strokes = Array.AsReadOnly(copiedStrokes);
    }

    public ImageSize SourceSize { get; }

    public ulong Seed { get; }

    public int FieldSeed { get; }

    public int ReferenceMaximumDimension { get; }

    public StrokePlanBackgroundMode BackgroundMode { get; }

    public string PlannerVersion { get; }

    public IReadOnlyList<FlowStroke> Strokes => _strokes;
}
