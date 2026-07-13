using FlowPainter.Application.Detail;
using FlowPainter.Domain.Brushes;
using FlowPainter.Application.FlowPainting.Fields;
using FlowPainter.Application.Semantics;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Strokes;

namespace FlowPainter.Application.FlowPainting.Planning;

public sealed class FlowPainterSettings
{
    public const int MaximumStrokeCount = 1_000_000;
    public const int MaximumSegmentCount = 1_024;
    public const int DefaultStrokeCount = 12_000;
    public const int DefaultSegmentCount = 20;
    public const int DefaultReferenceMaximumDimension = 512;
    public const double DefaultUniformDensity = 18d;
    public const double DefaultLengthScale = 0.005d;
    public const double DefaultMaximumCurveRadians = 0.5d;
    public const double DefaultMinimumStrokeWidthPixels = 3d;
    public const double DefaultMaximumStrokeWidthPixels = 7d;
    public const double DefaultStrokeOpacity = 0.85d;

    public FlowPainterSettings(
        FlowFieldSettings? field = null,
        int strokeCount = DefaultStrokeCount,
        int segmentCount = DefaultSegmentCount,
        int referenceMaximumDimension = DefaultReferenceMaximumDimension,
        double uniformDensity = DefaultUniformDensity,
        double lengthScale = DefaultLengthScale,
        double maximumCurveRadians = DefaultMaximumCurveRadians,
        double minimumStrokeWidthPixels = DefaultMinimumStrokeWidthPixels,
        double maximumStrokeWidthPixels = DefaultMaximumStrokeWidthPixels,
        double strokeOpacity = DefaultStrokeOpacity,
        StrokePlanBackgroundMode backgroundMode = StrokePlanBackgroundMode.SourceImage,
        DetailAnalysisSettings? detailAnalysis = null,
        DetailInfluenceSettings? detailInfluence = null,
        BrushSettings? brush = null,
        SemanticAnalysisSettings? semanticAnalysis = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(strokeCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(strokeCount, MaximumStrokeCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(segmentCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(segmentCount, MaximumSegmentCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(referenceMaximumDimension, 1);

        if (referenceMaximumDimension > ImageSize.MaximumDimension)
        {
            throw new ArgumentOutOfRangeException(
                nameof(referenceMaximumDimension),
                referenceMaximumDimension,
                $"The reference dimension cannot exceed {ImageSize.MaximumDimension:N0} pixels.");
        }

        ValidateFinitePositive(uniformDensity, nameof(uniformDensity));
        ValidateFinitePositive(lengthScale, nameof(lengthScale));

        if (!double.IsFinite(maximumCurveRadians)
            || maximumCurveRadians <= 0d
            || maximumCurveRadians > AngleMath.Tau)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumCurveRadians),
                maximumCurveRadians,
                $"Maximum curve must be finite and in the (0, {AngleMath.Tau}] range.");
        }

        ValidateFinitePositive(minimumStrokeWidthPixels, nameof(minimumStrokeWidthPixels));
        ValidateFinitePositive(maximumStrokeWidthPixels, nameof(maximumStrokeWidthPixels));

        if (maximumStrokeWidthPixels < minimumStrokeWidthPixels)
        {
            throw new ArgumentException(
                "Maximum stroke width must be greater than or equal to minimum stroke width.",
                nameof(maximumStrokeWidthPixels));
        }

        if (!double.IsFinite(strokeOpacity) || strokeOpacity <= 0d || strokeOpacity > 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(strokeOpacity),
                strokeOpacity,
                "Stroke opacity must be finite and in the (0, 1] range.");
        }

        if (!Enum.IsDefined(backgroundMode))
        {
            throw new ArgumentOutOfRangeException(nameof(backgroundMode), backgroundMode, "Unknown background mode.");
        }

        Field = field ?? new FlowFieldSettings();
        StrokeCount = strokeCount;
        SegmentCount = segmentCount;
        ReferenceMaximumDimension = referenceMaximumDimension;
        UniformDensity = uniformDensity;
        LengthScale = lengthScale;
        MaximumCurveRadians = maximumCurveRadians;
        MinimumStrokeWidthPixels = minimumStrokeWidthPixels;
        MaximumStrokeWidthPixels = maximumStrokeWidthPixels;
        StrokeOpacity = strokeOpacity;
        BackgroundMode = backgroundMode;
        DetailAnalysis = detailAnalysis ?? new DetailAnalysisSettings();
        DetailInfluence = detailInfluence ?? new DetailInfluenceSettings();
        Brush = brush ?? new BrushSettings();
        SemanticAnalysis = semanticAnalysis ?? new SemanticAnalysisSettings();
    }

    public FlowFieldSettings Field { get; }

    public int StrokeCount { get; }

    public int SegmentCount { get; }

    public int ReferenceMaximumDimension { get; }

    public double UniformDensity { get; }

    public double LengthScale { get; }

    public double MaximumCurveRadians { get; }

    public double MinimumStrokeWidthPixels { get; }

    public double MaximumStrokeWidthPixels { get; }

    public double StrokeOpacity { get; }

    public StrokePlanBackgroundMode BackgroundMode { get; }

    public DetailAnalysisSettings DetailAnalysis { get; }

    public DetailInfluenceSettings DetailInfluence { get; }

    public BrushSettings Brush { get; }

    public SemanticAnalysisSettings SemanticAnalysis { get; }

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
