namespace FlowPainter.Imaging.Skia.Images;

public readonly record struct ImageOperationProgress
{
    public ImageOperationProgress(
        ImageOperationStage stage,
        double fraction,
        string message)
    {
        if (!Enum.IsDefined(stage))
        {
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown image operation stage.");
        }

        if (!double.IsFinite(fraction) || fraction < 0d || fraction > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Progress must be between zero and one.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("A progress message is required.", nameof(message));
        }

        Stage = stage;
        Fraction = fraction;
        Message = message.Trim();
    }

    public ImageOperationStage Stage { get; }

    public double Fraction { get; }

    public string Message { get; }
}
