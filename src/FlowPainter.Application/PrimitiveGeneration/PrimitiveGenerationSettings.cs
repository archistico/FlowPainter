using FlowPainter.Domain.Primitives;

namespace FlowPainter.Application.PrimitiveGeneration;

public sealed class PrimitiveGenerationSettings
{
    public const int MaximumPrimitiveCount = 20_000;
    public const int MaximumCandidateCount = 512;
    public const int MaximumMutationIterations = 2_048;

    public PrimitiveGenerationSettings(
        int primitiveCount = 300,
        int candidatesPerStep = 20,
        int mutationIterations = 24,
        double minimumSize = 0.015d,
        double maximumSize = 0.22d,
        double opacity = 0.72d,
        double detailSizeInfluence = 0.8d,
        double detailPlacementBias = 2d,
        double detailErrorWeight = 1.5d,
        double detailSearchInfluence = 1d,
        PrimitiveKindSet allowedKinds = PrimitiveKindSet.All)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(primitiveCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(primitiveCount, MaximumPrimitiveCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(candidatesPerStep, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(candidatesPerStep, MaximumCandidateCount);
        ArgumentOutOfRangeException.ThrowIfNegative(mutationIterations);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(mutationIterations, MaximumMutationIterations);
        ValidateUnitSize(minimumSize, nameof(minimumSize));
        ValidateUnitSize(maximumSize, nameof(maximumSize));
        if (maximumSize < minimumSize)
        {
            throw new ArgumentException(
                "Maximum primitive size must be greater than or equal to minimum primitive size.",
                nameof(maximumSize));
        }

        ValidateUnitIntervalExclusiveZero(opacity, nameof(opacity));
        ValidateUnitInterval(detailSizeInfluence, nameof(detailSizeInfluence));
        ValidateFiniteNonNegative(detailPlacementBias, nameof(detailPlacementBias));
        ValidateFiniteNonNegative(detailErrorWeight, nameof(detailErrorWeight));
        ValidateFiniteNonNegative(detailSearchInfluence, nameof(detailSearchInfluence));
        if (detailSearchInfluence > 4d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(detailSearchInfluence),
                detailSearchInfluence,
                "Detail search influence cannot exceed 4.");
        }

        if (allowedKinds == PrimitiveKindSet.None || (allowedKinds & ~PrimitiveKindSet.All) != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(allowedKinds),
                allowedKinds,
                "At least one supported primitive kind must be enabled.");
        }

        PrimitiveCount = primitiveCount;
        CandidatesPerStep = candidatesPerStep;
        MutationIterations = mutationIterations;
        MinimumSize = minimumSize;
        MaximumSize = maximumSize;
        Opacity = opacity;
        DetailSizeInfluence = detailSizeInfluence;
        DetailPlacementBias = detailPlacementBias;
        DetailErrorWeight = detailErrorWeight;
        DetailSearchInfluence = detailSearchInfluence;
        AllowedKinds = allowedKinds;
    }

    public int PrimitiveCount { get; }

    public int CandidatesPerStep { get; }

    public int MutationIterations { get; }

    public double MinimumSize { get; }

    public double MaximumSize { get; }

    public double Opacity { get; }

    public double DetailSizeInfluence { get; }

    public double DetailPlacementBias { get; }

    public double DetailErrorWeight { get; }

    public double DetailSearchInfluence { get; }

    public PrimitiveKindSet AllowedKinds { get; }

    private static void ValidateUnitSize(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0d || value > 1d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Primitive size must be finite and in the (0, 1] range.");
        }
    }

    private static void ValidateUnitIntervalExclusiveZero(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0d || value > 1d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The value must be finite and in the (0, 1] range.");
        }
    }

    private static void ValidateUnitInterval(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d || value > 1d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The value must be finite and in the [0, 1] range.");
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
