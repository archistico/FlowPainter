using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Segmentation;

public static class RegionMergeCostModel
{
    public const double ColorDifferenceWeight = 0.30d;
    public const double TextureDifferenceWeight = 0.15d;
    public const double BoundaryStrengthWeight = 0.35d;
    public const double ShapePenaltyWeight = 0.10d;
    public const double ResultingSizePenaltyWeight = 0.10d;

    public const double ColorNormalizationScale = 30d;
    public const double TextureNormalizationScale = 15d;

    public static double Calculate(
        ImageRegion first,
        ImageRegion second,
        RegionAdjacency boundary,
        double maximumParentAreaFraction)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(boundary);
        if (!boundary.Connects(first.Id, second.Id))
        {
            throw new ArgumentException("The boundary must connect the supplied regions.", nameof(boundary));
        }

        return Calculate(
            first.PixelCount,
            first.NormalizedArea,
            first.Descriptors.Perimeter,
            first.Descriptors.MeanLightness,
            first.Descriptors.MeanA,
            first.Descriptors.MeanB,
            first.Descriptors.TextureEnergy,
            second.PixelCount,
            second.NormalizedArea,
            second.Descriptors.Perimeter,
            second.Descriptors.MeanLightness,
            second.Descriptors.MeanA,
            second.Descriptors.MeanB,
            second.Descriptors.TextureEnergy,
            boundary.SharedBoundaryLength,
            boundary.BoundaryStrength,
            maximumParentAreaFraction);
    }

    internal static double Calculate(
        int firstPixelCount,
        double firstNormalizedArea,
        double firstPerimeter,
        double firstMeanLightness,
        double firstMeanA,
        double firstMeanB,
        double firstTextureEnergy,
        int secondPixelCount,
        double secondNormalizedArea,
        double secondPerimeter,
        double secondMeanLightness,
        double secondMeanA,
        double secondMeanB,
        double secondTextureEnergy,
        int sharedBoundaryLength,
        double meanBoundaryStrength,
        double maximumParentAreaFraction)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(firstPixelCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(secondPixelCount);
        ValidatePositiveUnitInterval(firstNormalizedArea, nameof(firstNormalizedArea));
        ValidatePositiveUnitInterval(secondNormalizedArea, nameof(secondNormalizedArea));
        ValidateNonNegative(firstPerimeter, nameof(firstPerimeter));
        ValidateNonNegative(secondPerimeter, nameof(secondPerimeter));
        ValidateFinite(firstMeanLightness, nameof(firstMeanLightness));
        ValidateFinite(firstMeanA, nameof(firstMeanA));
        ValidateFinite(firstMeanB, nameof(firstMeanB));
        ValidateNonNegative(firstTextureEnergy, nameof(firstTextureEnergy));
        ValidateFinite(secondMeanLightness, nameof(secondMeanLightness));
        ValidateFinite(secondMeanA, nameof(secondMeanA));
        ValidateFinite(secondMeanB, nameof(secondMeanB));
        ValidateNonNegative(secondTextureEnergy, nameof(secondTextureEnergy));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sharedBoundaryLength);
        ValidateUnitInterval(meanBoundaryStrength, nameof(meanBoundaryStrength));
        ValidatePositiveUnitInterval(maximumParentAreaFraction, nameof(maximumParentAreaFraction));

        double lightnessDifference = firstMeanLightness - secondMeanLightness;
        double aDifference = firstMeanA - secondMeanA;
        double bDifference = firstMeanB - secondMeanB;
        double colorDifference = Math.Sqrt(
            (lightnessDifference * lightnessDifference)
            + (aDifference * aDifference)
            + (bDifference * bDifference));
        double textureDifference = Math.Abs(firstTextureEnergy - secondTextureEnergy);
        double combinedArea = firstNormalizedArea + secondNormalizedArea;
        double mergedPerimeter = Math.Max(
            1d,
            firstPerimeter + secondPerimeter - (2d * sharedBoundaryLength));
        double compactness = Math.Clamp(
            (4d * Math.PI * (firstPixelCount + secondPixelCount))
            / (mergedPerimeter * mergedPerimeter),
            0d,
            1d);
        double shapePenalty = 1d - compactness;
        double sizePenalty = Math.Clamp(
            combinedArea / maximumParentAreaFraction,
            0d,
            1d);

        double cost =
            (ColorDifferenceWeight * Normalize(colorDifference, ColorNormalizationScale))
            + (TextureDifferenceWeight * Normalize(textureDifference, TextureNormalizationScale))
            + (BoundaryStrengthWeight * meanBoundaryStrength)
            + (ShapePenaltyWeight * shapePenalty)
            + (ResultingSizePenaltyWeight * sizePenalty);
        return Math.Clamp(cost, 0d, 1d);
    }

    private static double Normalize(double value, double scale)
    {
        return value <= 0d ? 0d : value / (value + scale);
    }


    private static void ValidateFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The value must be finite.");
        }
    }

    private static void ValidateNonNegative(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The value must be finite and non-negative.");
        }
    }
    private static void ValidatePositiveUnitInterval(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0d || value > 1d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The value must be finite, greater than zero and no greater than one.");
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
