namespace FlowPainter.Application.Boundaries;

public sealed class BoundaryPaintingSettings
{
    public const double DefaultTangentAlignment = 0.78d;
    public const int DefaultAlignmentRadius = 5;
    public const double DefaultCrossingPenalty = 0.82d;
    public const double DefaultHardBoundaryThreshold = 0.62d;
    public const double DefaultTerminationStrength = 0.78d;
    public const double DefaultInternalEdgeInfluence = 0.42d;
    public const double DefaultTextureEdgeInfluence = 0.08d;
    public const double DefaultContourReinforcement = 0.55d;
    public const double DefaultCornerPreservation = 0.68d;
    public const int MaximumAlignmentRadius = 24;

    public BoundaryPaintingSettings(
        bool enabled = false,
        double tangentAlignment = DefaultTangentAlignment,
        int alignmentRadius = DefaultAlignmentRadius,
        double crossingPenalty = DefaultCrossingPenalty,
        double hardBoundaryThreshold = DefaultHardBoundaryThreshold,
        double terminationStrength = DefaultTerminationStrength,
        double internalEdgeInfluence = DefaultInternalEdgeInfluence,
        double textureEdgeInfluence = DefaultTextureEdgeInfluence,
        double contourReinforcement = DefaultContourReinforcement,
        double cornerPreservation = DefaultCornerPreservation)
    {
        ValidateUnitInterval(tangentAlignment, nameof(tangentAlignment));
        ArgumentOutOfRangeException.ThrowIfNegative(alignmentRadius);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            alignmentRadius,
            MaximumAlignmentRadius,
            nameof(alignmentRadius));
        ValidateUnitInterval(crossingPenalty, nameof(crossingPenalty));
        ValidateUnitInterval(hardBoundaryThreshold, nameof(hardBoundaryThreshold));
        ValidateUnitInterval(terminationStrength, nameof(terminationStrength));
        ValidateUnitInterval(internalEdgeInfluence, nameof(internalEdgeInfluence));
        ValidateUnitInterval(textureEdgeInfluence, nameof(textureEdgeInfluence));
        ValidateRange(contourReinforcement, 0d, 4d, nameof(contourReinforcement));
        ValidateUnitInterval(cornerPreservation, nameof(cornerPreservation));

        Enabled = enabled;
        TangentAlignment = tangentAlignment;
        AlignmentRadius = alignmentRadius;
        CrossingPenalty = crossingPenalty;
        HardBoundaryThreshold = hardBoundaryThreshold;
        TerminationStrength = terminationStrength;
        InternalEdgeInfluence = internalEdgeInfluence;
        TextureEdgeInfluence = textureEdgeInfluence;
        ContourReinforcement = contourReinforcement;
        CornerPreservation = cornerPreservation;
    }

    public bool Enabled { get; }

    public double TangentAlignment { get; }

    public int AlignmentRadius { get; }

    public double CrossingPenalty { get; }

    public double HardBoundaryThreshold { get; }

    public double TerminationStrength { get; }

    public double InternalEdgeInfluence { get; }

    public double TextureEdgeInfluence { get; }

    public double ContourReinforcement { get; }

    public double CornerPreservation { get; }

    private static void ValidateUnitInterval(double value, string parameterName)
    {
        ValidateRange(value, 0d, 1d, parameterName);
    }

    private static void ValidateRange(
        double value,
        double minimum,
        double maximum,
        string parameterName)
    {
        if (!double.IsFinite(value) || value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"The value must be finite and between {minimum} and {maximum}.");
        }
    }
}
