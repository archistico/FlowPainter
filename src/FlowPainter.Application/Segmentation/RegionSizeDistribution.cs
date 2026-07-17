namespace FlowPainter.Application.Segmentation;

public sealed class RegionSizeDistribution
{
    public RegionSizeDistribution(
        int minimumPixelCount,
        int maximumPixelCount,
        double meanPixelCount,
        double standardDeviationPixelCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimumPixelCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumPixelCount);
        if (maximumPixelCount < minimumPixelCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumPixelCount),
                maximumPixelCount,
                "The maximum region size cannot be smaller than the minimum region size.");
        }

        ValidateFinitePositive(meanPixelCount, nameof(meanPixelCount));
        ValidateFiniteNonNegative(standardDeviationPixelCount, nameof(standardDeviationPixelCount));
        if (meanPixelCount < minimumPixelCount || meanPixelCount > maximumPixelCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(meanPixelCount),
                meanPixelCount,
                "The mean region size must lie between the minimum and maximum sizes.");
        }

        MinimumPixelCount = minimumPixelCount;
        MaximumPixelCount = maximumPixelCount;
        MeanPixelCount = meanPixelCount;
        StandardDeviationPixelCount = standardDeviationPixelCount;
    }

    public int MinimumPixelCount { get; }

    public int MaximumPixelCount { get; }

    public double MeanPixelCount { get; }

    public double StandardDeviationPixelCount { get; }

    public static RegionSizeDistribution Create(ReadOnlySpan<int> pixelCounts)
    {
        if (pixelCounts.IsEmpty)
        {
            throw new ArgumentException("At least one region size is required.", nameof(pixelCounts));
        }

        int minimum = int.MaxValue;
        int maximum = 0;
        long total = 0L;
        for (int index = 0; index < pixelCounts.Length; index++)
        {
            int value = pixelCounts[index];
            if (value <= 0)
            {
                throw new ArgumentException(
                    "Every region size must be greater than zero.",
                    nameof(pixelCounts));
            }
            minimum = Math.Min(minimum, value);
            maximum = Math.Max(maximum, value);
            total = checked(total + value);
        }

        double mean = total / (double)pixelCounts.Length;
        double squaredDifferenceSum = 0d;
        for (int index = 0; index < pixelCounts.Length; index++)
        {
            double difference = pixelCounts[index] - mean;
            squaredDifferenceSum += difference * difference;
        }

        return new RegionSizeDistribution(
            minimum,
            maximum,
            mean,
            Math.Sqrt(squaredDifferenceSum / pixelCounts.Length));
    }

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

    private static void ValidateFiniteNonNegative(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The value must be finite and non-negative.");
        }
    }
}
