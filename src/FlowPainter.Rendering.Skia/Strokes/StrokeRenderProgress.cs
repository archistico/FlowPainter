namespace FlowPainter.Rendering.Skia.Strokes;

public readonly record struct StrokeRenderProgress
{
    public StrokeRenderProgress(
        StrokeRenderStage stage,
        int completedStrokes,
        int totalStrokes,
        double fraction)
    {
        if (!Enum.IsDefined(stage))
        {
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown render stage.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(completedStrokes);
        ArgumentOutOfRangeException.ThrowIfNegative(totalStrokes);
        if (completedStrokes > totalStrokes)
        {
            throw new ArgumentException("Completed strokes cannot exceed total strokes.", nameof(completedStrokes));
        }

        if (!double.IsFinite(fraction) || fraction < 0d || fraction > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Progress must be between zero and one.");
        }

        Stage = stage;
        CompletedStrokes = completedStrokes;
        TotalStrokes = totalStrokes;
        Fraction = fraction;
    }

    public StrokeRenderStage Stage { get; }

    public int CompletedStrokes { get; }

    public int TotalStrokes { get; }

    public double Fraction { get; }
}
