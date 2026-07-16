namespace FlowPainter.Domain.Segmentation;

public sealed class RegionVisualDescriptors
{
    public static RegionVisualDescriptors Empty { get; } = new();

    public RegionVisualDescriptors(
        double perimeter = 0d,
        double compactness = 0d,
        double meanLightness = 0d,
        double meanA = 0d,
        double meanB = 0d,
        double lightnessVariance = 0d,
        double aVariance = 0d,
        double bVariance = 0d,
        double textureEnergy = 0d,
        double edgeDensity = 0d,
        double dominantOrientationRadians = 0d)
    {
        ValidateNonNegative(perimeter, nameof(perimeter));
        ValidateUnitInterval(compactness, nameof(compactness));
        ValidateRange(meanLightness, 0d, 100d, nameof(meanLightness));
        ValidateFinite(meanA, nameof(meanA));
        ValidateFinite(meanB, nameof(meanB));
        ValidateNonNegative(lightnessVariance, nameof(lightnessVariance));
        ValidateNonNegative(aVariance, nameof(aVariance));
        ValidateNonNegative(bVariance, nameof(bVariance));
        ValidateNonNegative(textureEnergy, nameof(textureEnergy));
        ValidateUnitInterval(edgeDensity, nameof(edgeDensity));
        ValidateFinite(dominantOrientationRadians, nameof(dominantOrientationRadians));

        Perimeter = perimeter;
        Compactness = compactness;
        MeanLightness = meanLightness;
        MeanA = meanA;
        MeanB = meanB;
        LightnessVariance = lightnessVariance;
        AVariance = aVariance;
        BVariance = bVariance;
        TextureEnergy = textureEnergy;
        EdgeDensity = edgeDensity;
        DominantOrientationRadians = dominantOrientationRadians;
    }

    public double Perimeter { get; }

    public double Compactness { get; }

    public double MeanLightness { get; }

    public double MeanA { get; }

    public double MeanB { get; }

    public double LightnessVariance { get; }

    public double AVariance { get; }

    public double BVariance { get; }

    public double TextureEnergy { get; }

    public double EdgeDensity { get; }

    public double DominantOrientationRadians { get; }

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
        ValidateRange(value, 0d, 1d, parameterName);
    }

    private static void ValidateRange(double value, double minimum, double maximum, string parameterName)
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
