namespace FlowPainter.Application.Semantics;

public sealed class SemanticAnalysisSettings
{
    public const double DefaultOverallInfluence = 0.7d;
    public const double DefaultSaliencyWeight = 0.35d;
    public const double DefaultSubjectWeight = 0.8d;
    public const double DefaultSilhouetteWeight = 0.9d;
    public const double DefaultFocalWeight = 1d;
    public const double DefaultSubjectThreshold = 0.52d;
    public const double DefaultMinimumSubjectAreaRatio = 0.004d;
    public const int DefaultMaximumSubjects = 6;
    public const double DefaultCenterBias = 0.25d;
    public const int DefaultSmoothingRadius = 2;
    public const int DefaultBoundaryRadius = 2;
    public const int MaximumRadius = 16;
    public const int MaximumSubjectCount = 32;

    public SemanticAnalysisSettings(
        bool enabled = true,
        double overallInfluence = DefaultOverallInfluence,
        double saliencyWeight = DefaultSaliencyWeight,
        double subjectWeight = DefaultSubjectWeight,
        double silhouetteWeight = DefaultSilhouetteWeight,
        double focalWeight = DefaultFocalWeight,
        double subjectThreshold = DefaultSubjectThreshold,
        double minimumSubjectAreaRatio = DefaultMinimumSubjectAreaRatio,
        int maximumSubjects = DefaultMaximumSubjects,
        double centerBias = DefaultCenterBias,
        int smoothingRadius = DefaultSmoothingRadius,
        int boundaryRadius = DefaultBoundaryRadius)
    {
        ValidateUnitInterval(overallInfluence, nameof(overallInfluence));
        ValidateWeight(saliencyWeight, nameof(saliencyWeight));
        ValidateWeight(subjectWeight, nameof(subjectWeight));
        ValidateWeight(silhouetteWeight, nameof(silhouetteWeight));
        ValidateWeight(focalWeight, nameof(focalWeight));
        ValidateUnitInterval(subjectThreshold, nameof(subjectThreshold));

        if (!double.IsFinite(minimumSubjectAreaRatio)
            || minimumSubjectAreaRatio <= 0d
            || minimumSubjectAreaRatio > 0.25d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumSubjectAreaRatio),
                minimumSubjectAreaRatio,
                "Minimum subject area ratio must be finite and in the (0, 0.25] range.");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(maximumSubjects, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumSubjects, MaximumSubjectCount);

        if (!double.IsFinite(centerBias) || centerBias < 0d || centerBias > 2d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(centerBias),
                centerBias,
                "Center bias must be finite and between 0 and 2.");
        }

        ValidateRadius(smoothingRadius, nameof(smoothingRadius));
        ValidateRadius(boundaryRadius, nameof(boundaryRadius));

        if (enabled
            && saliencyWeight == 0d
            && subjectWeight == 0d
            && silhouetteWeight == 0d
            && focalWeight == 0d)
        {
            throw new ArgumentException(
                "At least one semantic-map weight must be greater than zero when semantic analysis is enabled.",
                nameof(saliencyWeight));
        }

        Enabled = enabled;
        OverallInfluence = overallInfluence;
        SaliencyWeight = saliencyWeight;
        SubjectWeight = subjectWeight;
        SilhouetteWeight = silhouetteWeight;
        FocalWeight = focalWeight;
        SubjectThreshold = subjectThreshold;
        MinimumSubjectAreaRatio = minimumSubjectAreaRatio;
        MaximumSubjects = maximumSubjects;
        CenterBias = centerBias;
        SmoothingRadius = smoothingRadius;
        BoundaryRadius = boundaryRadius;
    }

    public bool Enabled { get; }

    public double OverallInfluence { get; }

    public double SaliencyWeight { get; }

    public double SubjectWeight { get; }

    public double SilhouetteWeight { get; }

    public double FocalWeight { get; }

    public double SubjectThreshold { get; }

    public double MinimumSubjectAreaRatio { get; }

    public int MaximumSubjects { get; }

    public double CenterBias { get; }

    public int SmoothingRadius { get; }

    public int BoundaryRadius { get; }

    private static void ValidateUnitInterval(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d || value > 1d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The value must be finite and between 0 and 1.");
        }
    }

    private static void ValidateWeight(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d || value > 4d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The weight must be finite and between 0 and 4.");
        }
    }

    private static void ValidateRadius(int value, string parameterName)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value, parameterName);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaximumRadius, parameterName);
    }
}
