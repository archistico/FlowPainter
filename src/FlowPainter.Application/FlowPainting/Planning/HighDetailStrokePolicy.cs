using FlowPainter.Application.Boundaries;

namespace FlowPainter.Application.FlowPainting.Planning;

public static class HighDetailStrokePolicy
{
    public static LocalStrokeGeometry EvaluateGeometry(
        double detail,
        DetailInfluenceSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ValidateDetail(detail);
        return new LocalStrokeGeometry(
            settings.GetLengthMultiplier(detail),
            settings.GetWidthMultiplier(detail),
            settings.GetSegmentMultiplier(detail),
            settings.GetCurveMultiplier(detail));
    }

    public static double GetTangentAlignmentAmount(
        double detail,
        BoundaryGuidanceSample guidance,
        DetailInfluenceSettings detailSettings,
        BoundaryPaintingSettings boundarySettings)
    {
        ArgumentNullException.ThrowIfNull(detailSettings);
        ArgumentNullException.ThrowIfNull(boundarySettings);
        ValidateDetail(detail);
        if (!guidance.HasDirection)
        {
            return 0d;
        }

        double evidence = GetBoundaryEvidence(guidance);
        double detailWeight = SmoothStep(detail) * evidence;
        return Math.Clamp(
            (boundarySettings.TangentAlignment * guidance.Influence)
            + (detailSettings.DetailedTangentAlignmentBoost * detailWeight),
            0d,
            1d);
    }

    public static double GetCrossingResistance(
        double detail,
        BoundaryGuidanceSample guidance,
        DetailInfluenceSettings detailSettings,
        BoundaryPaintingSettings boundarySettings)
    {
        ArgumentNullException.ThrowIfNull(detailSettings);
        ArgumentNullException.ThrowIfNull(boundarySettings);
        ValidateDetail(detail);
        double evidence = GetBoundaryEvidence(guidance);
        double detailWeight = SmoothStep(detail) * evidence;
        return Math.Clamp(
            boundarySettings.CrossingPenalty
            + (detailSettings.DetailedCrossingResistanceBoost * detailWeight),
            0d,
            1d);
    }

    private static double GetBoundaryEvidence(BoundaryGuidanceSample guidance)
    {
        return Math.Clamp(
            Math.Max(guidance.Influence, guidance.RegionalBoundaryStrength),
            0d,
            1d);
    }

    private static double SmoothStep(double value)
    {
        return value * value * (3d - (2d * value));
    }

    private static void ValidateDetail(double detail)
    {
        if (!double.IsFinite(detail) || detail < 0d || detail > 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(detail),
                detail,
                "Detail must be finite and between 0 and 1.");
        }
    }
}
