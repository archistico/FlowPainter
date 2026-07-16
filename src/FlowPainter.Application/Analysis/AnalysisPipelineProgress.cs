namespace FlowPainter.Application.Analysis;

public sealed record AnalysisPipelineProgress
{
    public AnalysisPipelineProgress(
        AnalysisPipelineStage stage,
        double fraction,
        string message)
    {
        if (!Enum.IsDefined(stage))
        {
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown analysis-pipeline stage.");
        }

        if (!double.IsFinite(fraction) || fraction < 0d || fraction > 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fraction),
                fraction,
                "Analysis progress must be finite and between 0 and 1.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("An analysis progress message is required.", nameof(message));
        }

        Stage = stage;
        Fraction = fraction;
        Message = message.Trim();
    }

    public AnalysisPipelineStage Stage { get; }

    public double Fraction { get; }

    public string Message { get; }
}
