using FlowPainter.Domain.Images;

namespace FlowPainter.Application.FlowPainting.Legacy;

public sealed class LegacyFlowPainterSettings
{
    public const int DefaultStrokeCount = 50_000;
    public const int DefaultSegmentCount = 20;
    public const int DefaultReferenceMaximumDimension = 512;
    public const double DefaultNoiseScale = 1d;
    public const double DefaultLengthScale = 0.005d;
    public const double DefaultMaximumCurveRadians = 0.5d;
    public const double DefaultMinimumStrokeWidthPixels = 5d;
    public const double DefaultMaximumStrokeWidthPixels = 10d;

    public LegacyFlowPainterSettings(
        int strokeCount = DefaultStrokeCount,
        int segmentCount = DefaultSegmentCount,
        int referenceMaximumDimension = DefaultReferenceMaximumDimension,
        double noiseScale = DefaultNoiseScale,
        double lengthScale = DefaultLengthScale,
        double maximumCurveRadians = DefaultMaximumCurveRadians,
        double minimumStrokeWidthPixels = DefaultMinimumStrokeWidthPixels,
        double maximumStrokeWidthPixels = DefaultMaximumStrokeWidthPixels)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(strokeCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(segmentCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(referenceMaximumDimension);

        if (referenceMaximumDimension > ImageSize.MaximumDimension)
        {
            throw new ArgumentOutOfRangeException(
                nameof(referenceMaximumDimension),
                referenceMaximumDimension,
                $"The reference dimension cannot exceed {ImageSize.MaximumDimension:N0} pixels.");
        }

        ValidateFinitePositive(noiseScale, nameof(noiseScale));
        ValidateFinitePositive(lengthScale, nameof(lengthScale));
        ValidateFinitePositive(maximumCurveRadians, nameof(maximumCurveRadians));
        ValidateFinitePositive(minimumStrokeWidthPixels, nameof(minimumStrokeWidthPixels));
        ValidateFinitePositive(maximumStrokeWidthPixels, nameof(maximumStrokeWidthPixels));

        if (maximumStrokeWidthPixels < minimumStrokeWidthPixels)
        {
            throw new ArgumentException(
                "Maximum stroke width must be greater than or equal to minimum stroke width.",
                nameof(maximumStrokeWidthPixels));
        }

        StrokeCount = strokeCount;
        SegmentCount = segmentCount;
        ReferenceMaximumDimension = referenceMaximumDimension;
        NoiseScale = noiseScale;
        LengthScale = lengthScale;
        MaximumCurveRadians = maximumCurveRadians;
        MinimumStrokeWidthPixels = minimumStrokeWidthPixels;
        MaximumStrokeWidthPixels = maximumStrokeWidthPixels;
    }

    public int StrokeCount { get; }

    public int SegmentCount { get; }

    public int ReferenceMaximumDimension { get; }

    public double NoiseScale { get; }

    public double LengthScale { get; }

    public double MaximumCurveRadians { get; }

    public double MinimumStrokeWidthPixels { get; }

    public double MaximumStrokeWidthPixels { get; }

    private static void ValidateFinitePositive(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The value must be finite and greater than zero.");
        }
    }
}
