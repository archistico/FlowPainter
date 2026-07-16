using FlowPainter.Domain.Hybrid;

namespace FlowPainter.Application.Hybrid;

public sealed class HybridGenerationSettings
{
    public const double DefaultPrimitiveBudgetFraction = 0.35d;
    public const double DefaultFlowBudgetFraction = 0.45d;
    public const double DefaultRefinementBudgetFraction = 0.20d;
    public const double DefaultInfluenceStrength = 0.68d;
    public const double DefaultInfluenceRadiusMultiplier = 1.8d;
    public const int DefaultMaximumInfluencesPerSample = 12;
    public const double DefaultRefinementDetailBias = 3d;
    public const double DefaultRefinementLengthMultiplier = 0.45d;
    public const double DefaultRefinementWidthMultiplier = 0.55d;
    private const double BudgetTolerance = 0.000_001d;

    public HybridGenerationSettings(
        double primitiveBudgetFraction = DefaultPrimitiveBudgetFraction,
        double flowBudgetFraction = DefaultFlowBudgetFraction,
        double refinementBudgetFraction = DefaultRefinementBudgetFraction,
        PrimitiveFlowInfluenceKind influenceKind = PrimitiveFlowInfluenceKind.Mixed,
        double influenceStrength = DefaultInfluenceStrength,
        double influenceRadiusMultiplier = DefaultInfluenceRadiusMultiplier,
        int maximumInfluencesPerSample = DefaultMaximumInfluencesPerSample,
        double refinementDetailBias = DefaultRefinementDetailBias,
        double refinementLengthMultiplier = DefaultRefinementLengthMultiplier,
        double refinementWidthMultiplier = DefaultRefinementWidthMultiplier)
    {
        ValidateBudget(primitiveBudgetFraction, nameof(primitiveBudgetFraction), allowZero: false);
        ValidateBudget(flowBudgetFraction, nameof(flowBudgetFraction), allowZero: false);
        ValidateBudget(refinementBudgetFraction, nameof(refinementBudgetFraction), allowZero: false);
        double totalBudget = primitiveBudgetFraction + flowBudgetFraction + refinementBudgetFraction;
        if (Math.Abs(totalBudget - 1d) > BudgetTolerance)
        {
            throw new ArgumentException("Hybrid budget fractions must add up to one.");
        }

        if (!Enum.IsDefined(influenceKind))
        {
            throw new ArgumentOutOfRangeException(nameof(influenceKind), influenceKind, "Unknown primitive flow influence kind.");
        }

        ValidateRange(influenceStrength, 0d, 1d, nameof(influenceStrength));
        ValidateRange(influenceRadiusMultiplier, 0.1d, 10d, nameof(influenceRadiusMultiplier));
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumInfluencesPerSample, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumInfluencesPerSample, 64);
        ValidateRange(refinementDetailBias, 0d, 20d, nameof(refinementDetailBias));
        ValidateRange(refinementLengthMultiplier, 0.05d, 2d, nameof(refinementLengthMultiplier));
        ValidateRange(refinementWidthMultiplier, 0.05d, 2d, nameof(refinementWidthMultiplier));

        PrimitiveBudgetFraction = primitiveBudgetFraction;
        FlowBudgetFraction = flowBudgetFraction;
        RefinementBudgetFraction = refinementBudgetFraction;
        InfluenceKind = influenceKind;
        InfluenceStrength = influenceStrength;
        InfluenceRadiusMultiplier = influenceRadiusMultiplier;
        MaximumInfluencesPerSample = maximumInfluencesPerSample;
        RefinementDetailBias = refinementDetailBias;
        RefinementLengthMultiplier = refinementLengthMultiplier;
        RefinementWidthMultiplier = refinementWidthMultiplier;
    }

    public double PrimitiveBudgetFraction { get; }

    public double FlowBudgetFraction { get; }

    public double RefinementBudgetFraction { get; }

    public PrimitiveFlowInfluenceKind InfluenceKind { get; }

    public double InfluenceStrength { get; }

    public double InfluenceRadiusMultiplier { get; }

    public int MaximumInfluencesPerSample { get; }

    public double RefinementDetailBias { get; }

    public double RefinementLengthMultiplier { get; }

    public double RefinementWidthMultiplier { get; }

    private static void ValidateBudget(double value, string parameterName, bool allowZero)
    {
        double minimum = allowZero ? 0d : double.Epsilon;
        if (!double.IsFinite(value) || value < minimum || value > 1d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                allowZero
                    ? "A hybrid budget fraction must be finite and in the [0, 1] range."
                    : "A hybrid budget fraction must be finite and in the (0, 1] range.");
        }
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
