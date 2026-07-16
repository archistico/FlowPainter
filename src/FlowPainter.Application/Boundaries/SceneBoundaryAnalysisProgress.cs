namespace FlowPainter.Application.Boundaries;

public readonly record struct SceneBoundaryAnalysisProgress
{
    public SceneBoundaryAnalysisProgress(
        SceneBoundaryAnalysisStage stage,
        int completedRows,
        int totalRows,
        double fraction)
    {
        if (!Enum.IsDefined(stage))
        {
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown scene-boundary analysis stage.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(completedRows);
        ArgumentOutOfRangeException.ThrowIfNegative(totalRows);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(completedRows, totalRows);

        if (!double.IsFinite(fraction) || fraction < 0d || fraction > 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fraction),
                fraction,
                "Progress fraction must be finite and between 0 and 1.");
        }

        Stage = stage;
        CompletedRows = completedRows;
        TotalRows = totalRows;
        Fraction = fraction;
    }

    public SceneBoundaryAnalysisStage Stage { get; }

    public int CompletedRows { get; }

    public int TotalRows { get; }

    public double Fraction { get; }
}
