namespace FlowPainter.Application.Boundaries;

public sealed class SceneBoundaryAnalysisSettings
{
    public const double DefaultLuminanceWeight = 0.45d;
    public const double DefaultColorWeight = 0.75d;
    public const double DefaultMultiscaleWeight = 0.7d;
    public const double DefaultContinuityWeight = 0.85d;
    public const double DefaultSemanticBoundaryWeight = 1.2d;
    public const double DefaultTextureSuppression = 0.65d;
    public const double DefaultEdgeThreshold = 0.1d;
    public const double DefaultImportantEdgeThreshold = 0.32d;
    public const int DefaultCoarseRadius = 3;
    public const int DefaultSmoothingRadius = 1;
    public const int DefaultBoundaryProtectionRadius = 4;
    public const int MaximumRadius = 24;

    public SceneBoundaryAnalysisSettings(
        bool enabled = true,
        double luminanceWeight = DefaultLuminanceWeight,
        double colorWeight = DefaultColorWeight,
        double multiscaleWeight = DefaultMultiscaleWeight,
        double continuityWeight = DefaultContinuityWeight,
        double semanticBoundaryWeight = DefaultSemanticBoundaryWeight,
        double textureSuppression = DefaultTextureSuppression,
        double edgeThreshold = DefaultEdgeThreshold,
        double importantEdgeThreshold = DefaultImportantEdgeThreshold,
        int coarseRadius = DefaultCoarseRadius,
        int smoothingRadius = DefaultSmoothingRadius,
        int boundaryProtectionRadius = DefaultBoundaryProtectionRadius)
    {
        ValidateWeight(luminanceWeight, nameof(luminanceWeight));
        ValidateWeight(colorWeight, nameof(colorWeight));
        ValidateWeight(multiscaleWeight, nameof(multiscaleWeight));
        ValidateWeight(continuityWeight, nameof(continuityWeight));
        ValidateWeight(semanticBoundaryWeight, nameof(semanticBoundaryWeight));
        ValidateUnitInterval(textureSuppression, nameof(textureSuppression));
        ValidateUnitInterval(edgeThreshold, nameof(edgeThreshold));
        ValidateUnitInterval(importantEdgeThreshold, nameof(importantEdgeThreshold));

        if (importantEdgeThreshold < edgeThreshold)
        {
            throw new ArgumentException(
                "The important-edge threshold must be greater than or equal to the general edge threshold.",
                nameof(importantEdgeThreshold));
        }

        ValidatePositiveRadius(coarseRadius, nameof(coarseRadius));
        ValidateRadius(smoothingRadius, nameof(smoothingRadius));
        ValidateRadius(boundaryProtectionRadius, nameof(boundaryProtectionRadius));

        if (enabled && luminanceWeight == 0d && colorWeight == 0d)
        {
            throw new ArgumentException(
                "At least one edge signal must have a positive weight when boundary analysis is enabled.",
                nameof(luminanceWeight));
        }

        Enabled = enabled;
        LuminanceWeight = luminanceWeight;
        ColorWeight = colorWeight;
        MultiscaleWeight = multiscaleWeight;
        ContinuityWeight = continuityWeight;
        SemanticBoundaryWeight = semanticBoundaryWeight;
        TextureSuppression = textureSuppression;
        EdgeThreshold = edgeThreshold;
        ImportantEdgeThreshold = importantEdgeThreshold;
        CoarseRadius = coarseRadius;
        SmoothingRadius = smoothingRadius;
        BoundaryProtectionRadius = boundaryProtectionRadius;
    }

    public bool Enabled { get; }

    public double LuminanceWeight { get; }

    public double ColorWeight { get; }

    public double MultiscaleWeight { get; }

    public double ContinuityWeight { get; }

    public double SemanticBoundaryWeight { get; }

    public double TextureSuppression { get; }

    public double EdgeThreshold { get; }

    public double ImportantEdgeThreshold { get; }

    public int CoarseRadius { get; }

    public int SmoothingRadius { get; }

    public int BoundaryProtectionRadius { get; }

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

    private static void ValidatePositiveRadius(int value, string parameterName)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, parameterName);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaximumRadius, parameterName);
    }

    private static void ValidateRadius(int value, string parameterName)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value, parameterName);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaximumRadius, parameterName);
    }
}
