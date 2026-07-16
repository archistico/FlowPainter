namespace FlowPainter.Domain.Segmentation;

public sealed class RegionAdjacency
{
    public RegionAdjacency(
        int firstRegionId,
        int secondRegionId,
        int sharedBoundaryLength,
        double meanGradient = 0d,
        double maximumGradient = 0d,
        double colorDifference = 0d,
        double luminanceDifference = 0d,
        double textureDifference = 0d,
        double continuity = 0d,
        double prevailingTangentRadians = 0d,
        double boundaryStrength = 0d)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(firstRegionId);
        ArgumentOutOfRangeException.ThrowIfNegative(secondRegionId);
        if (firstRegionId >= secondRegionId)
        {
            throw new ArgumentException(
                "Adjacency identifiers must be distinct and ordered from lower to higher.",
                nameof(secondRegionId));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sharedBoundaryLength);
        ValidateNonNegative(meanGradient, nameof(meanGradient));
        ValidateNonNegative(maximumGradient, nameof(maximumGradient));
        if (maximumGradient < meanGradient)
        {
            throw new ArgumentException(
                "The maximum gradient cannot be lower than the mean gradient.",
                nameof(maximumGradient));
        }

        ValidateNonNegative(colorDifference, nameof(colorDifference));
        ValidateNonNegative(luminanceDifference, nameof(luminanceDifference));
        ValidateNonNegative(textureDifference, nameof(textureDifference));
        ValidateUnitInterval(continuity, nameof(continuity));
        ValidateFinite(prevailingTangentRadians, nameof(prevailingTangentRadians));
        ValidateUnitInterval(boundaryStrength, nameof(boundaryStrength));

        FirstRegionId = firstRegionId;
        SecondRegionId = secondRegionId;
        SharedBoundaryLength = sharedBoundaryLength;
        MeanGradient = meanGradient;
        MaximumGradient = maximumGradient;
        ColorDifference = colorDifference;
        LuminanceDifference = luminanceDifference;
        TextureDifference = textureDifference;
        Continuity = continuity;
        PrevailingTangentRadians = prevailingTangentRadians;
        BoundaryStrength = boundaryStrength;
    }

    public int FirstRegionId { get; }

    public int SecondRegionId { get; }

    public int SharedBoundaryLength { get; }

    public double MeanGradient { get; }

    public double MaximumGradient { get; }

    public double ColorDifference { get; }

    public double LuminanceDifference { get; }

    public double TextureDifference { get; }

    public double Continuity { get; }

    public double PrevailingTangentRadians { get; }

    public double BoundaryStrength { get; }

    public bool Connects(int firstRegionId, int secondRegionId)
    {
        return (FirstRegionId == firstRegionId && SecondRegionId == secondRegionId)
            || (FirstRegionId == secondRegionId && SecondRegionId == firstRegionId);
    }

    private static void ValidateFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "The value must be finite.");
        }
    }

    private static void ValidateNonNegative(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "The value must be finite and non-negative.");
        }
    }

    private static void ValidateUnitInterval(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d || value > 1d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The value must be finite and between zero and one.");
        }
    }
}
