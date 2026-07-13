namespace FlowPainter.Application.FlowPainting.Planning;

public readonly record struct StrokePlanningProgress
{
    public StrokePlanningProgress(
        StrokePlanningStage stage,
        int completedStrokes,
        int totalStrokes,
        double fraction)
    {
        if (!Enum.IsDefined(stage))
        {
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown planning stage.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(completedStrokes);
        ArgumentOutOfRangeException.ThrowIfNegative(totalStrokes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(completedStrokes, totalStrokes);

        if (!double.IsFinite(fraction) || fraction < 0d || fraction > 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fraction),
                fraction,
                "Progress fraction must be in the [0, 1] range.");
        }

        Stage = stage;
        CompletedStrokes = completedStrokes;
        TotalStrokes = totalStrokes;
        Fraction = fraction;
    }

    public StrokePlanningStage Stage { get; }

    public int CompletedStrokes { get; }

    public int TotalStrokes { get; }

    public double Fraction { get; }
}
