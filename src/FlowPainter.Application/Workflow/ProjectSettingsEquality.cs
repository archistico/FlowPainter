using FlowPainter.Application.Background;
using FlowPainter.Application.Boundaries;
using FlowPainter.Application.Detail;
using FlowPainter.Application.FlowPainting.Fields;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.Hybrid;
using FlowPainter.Application.PrimitiveGeneration;
using FlowPainter.Application.Semantics;
using FlowPainter.Application.Segmentation;
using FlowPainter.Domain.Brushes;

namespace FlowPainter.Application.Workflow;

internal static class ProjectSettingsEquality
{
    public static bool AreEquivalent(FlowPainterSettings first, FlowPainterSettings second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        return FlowFieldsAreEquivalent(first.Field, second.Field)
            && first.StrokeCount == second.StrokeCount
            && first.SegmentCount == second.SegmentCount
            && first.ReferenceMaximumDimension == second.ReferenceMaximumDimension
            && first.UniformDensity == second.UniformDensity
            && first.LengthScale == second.LengthScale
            && first.MaximumCurveRadians == second.MaximumCurveRadians
            && first.MinimumStrokeWidthPixels == second.MinimumStrokeWidthPixels
            && first.MaximumStrokeWidthPixels == second.MaximumStrokeWidthPixels
            && first.StrokeOpacity == second.StrokeOpacity
            && first.BackgroundMode == second.BackgroundMode
            && DetailAnalysisIsEquivalent(first.DetailAnalysis, second.DetailAnalysis)
            && DetailInfluenceIsEquivalent(first.DetailInfluence, second.DetailInfluence)
            && BrushesAreEquivalent(first.Brush, second.Brush)
            && SemanticAnalysisIsEquivalent(first.SemanticAnalysis, second.SemanticAnalysis)
            && BoundaryAnalysisIsEquivalent(first.BoundaryAnalysis, second.BoundaryAnalysis)
            && BoundaryPaintingIsEquivalent(first.BoundaryPainting, second.BoundaryPainting)
            && BackgroundSuppressionIsEquivalent(first.BackgroundSuppression, second.BackgroundSuppression)
            && RegionalSegmentationIsEquivalent(first.RegionalSegmentation, second.RegionalSegmentation)
            && RegionMergeIsEquivalent(first.RegionMerge, second.RegionMerge);
    }

    public static bool AreEquivalent(PrimitiveGenerationSettings first, PrimitiveGenerationSettings second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        return first.PrimitiveCount == second.PrimitiveCount
            && first.CandidatesPerStep == second.CandidatesPerStep
            && first.MutationIterations == second.MutationIterations
            && first.MinimumSize == second.MinimumSize
            && first.MaximumSize == second.MaximumSize
            && first.Opacity == second.Opacity
            && first.DetailSizeInfluence == second.DetailSizeInfluence
            && first.DetailPlacementBias == second.DetailPlacementBias
            && first.DetailErrorWeight == second.DetailErrorWeight
            && first.DetailSearchInfluence == second.DetailSearchInfluence
            && first.AllowedKinds == second.AllowedKinds;
    }

    public static bool AreEquivalent(HybridGenerationSettings first, HybridGenerationSettings second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        return first.PrimitiveBudgetFraction == second.PrimitiveBudgetFraction
            && first.FlowBudgetFraction == second.FlowBudgetFraction
            && first.RefinementBudgetFraction == second.RefinementBudgetFraction
            && first.InfluenceKind == second.InfluenceKind
            && first.InfluenceStrength == second.InfluenceStrength
            && first.InfluenceRadiusMultiplier == second.InfluenceRadiusMultiplier
            && first.MaximumInfluencesPerSample == second.MaximumInfluencesPerSample
            && first.RefinementDetailBias == second.RefinementDetailBias
            && first.RefinementLengthMultiplier == second.RefinementLengthMultiplier
            && first.RefinementWidthMultiplier == second.RefinementWidthMultiplier;
    }

    private static bool FlowFieldsAreEquivalent(FlowFieldSettings first, FlowFieldSettings second)
    {
        return first.Kind == second.Kind
            && first.Scale == second.Scale
            && first.Octaves == second.Octaves
            && first.Persistence == second.Persistence
            && first.Lacunarity == second.Lacunarity
            && first.AngleOffsetRadians == second.AngleOffsetRadians;
    }

    private static bool BrushesAreEquivalent(BrushSettings first, BrushSettings second)
    {
        return first.Kind == second.Kind
            && first.Hardness == second.Hardness
            && first.SizeJitter == second.SizeJitter
            && first.OpacityJitter == second.OpacityJitter
            && first.BristleCount == second.BristleCount
            && first.BristleSpread == second.BristleSpread;
    }

    private static bool DetailAnalysisIsEquivalent(DetailAnalysisSettings first, DetailAnalysisSettings second)
    {
        return first.BaseDetail == second.BaseDetail
            && first.EdgeWeight == second.EdgeWeight
            && first.ContrastWeight == second.ContrastWeight
            && first.SmoothingRadius == second.SmoothingRadius;
    }

    private static bool DetailInfluenceIsEquivalent(DetailInfluenceSettings first, DetailInfluenceSettings second)
    {
        return first.PlacementBias == second.PlacementBias
            && first.DetailedLengthMultiplier == second.DetailedLengthMultiplier
            && first.BackgroundLengthMultiplier == second.BackgroundLengthMultiplier
            && first.DetailedWidthMultiplier == second.DetailedWidthMultiplier
            && first.BackgroundWidthMultiplier == second.BackgroundWidthMultiplier
            && first.RegionTransitionWidth == second.RegionTransitionWidth
            && first.DetailedSegmentMultiplier == second.DetailedSegmentMultiplier
            && first.BackgroundSegmentMultiplier == second.BackgroundSegmentMultiplier
            && first.DetailedCurveMultiplier == second.DetailedCurveMultiplier
            && first.BackgroundCurveMultiplier == second.BackgroundCurveMultiplier
            && first.DetailedTangentAlignmentBoost == second.DetailedTangentAlignmentBoost
            && first.DetailedCrossingResistanceBoost == second.DetailedCrossingResistanceBoost;
    }

    private static bool SemanticAnalysisIsEquivalent(SemanticAnalysisSettings first, SemanticAnalysisSettings second)
    {
        return first.Enabled == second.Enabled
            && first.OverallInfluence == second.OverallInfluence
            && first.SaliencyWeight == second.SaliencyWeight
            && first.SubjectWeight == second.SubjectWeight
            && first.SilhouetteWeight == second.SilhouetteWeight
            && first.FocalWeight == second.FocalWeight
            && first.SubjectThreshold == second.SubjectThreshold
            && first.MinimumSubjectAreaRatio == second.MinimumSubjectAreaRatio
            && first.MaximumSubjects == second.MaximumSubjects
            && first.CenterBias == second.CenterBias
            && first.SmoothingRadius == second.SmoothingRadius
            && first.BoundaryRadius == second.BoundaryRadius;
    }

    private static bool BoundaryAnalysisIsEquivalent(SceneBoundaryAnalysisSettings first, SceneBoundaryAnalysisSettings second)
    {
        return first.Enabled == second.Enabled
            && first.LuminanceWeight == second.LuminanceWeight
            && first.ColorWeight == second.ColorWeight
            && first.MultiscaleWeight == second.MultiscaleWeight
            && first.ContinuityWeight == second.ContinuityWeight
            && first.SemanticBoundaryWeight == second.SemanticBoundaryWeight
            && first.TextureSuppression == second.TextureSuppression
            && first.EdgeThreshold == second.EdgeThreshold
            && first.ImportantEdgeThreshold == second.ImportantEdgeThreshold
            && first.CoarseRadius == second.CoarseRadius
            && first.SmoothingRadius == second.SmoothingRadius
            && first.BoundaryProtectionRadius == second.BoundaryProtectionRadius;
    }

    private static bool BoundaryPaintingIsEquivalent(BoundaryPaintingSettings first, BoundaryPaintingSettings second)
    {
        return first.Enabled == second.Enabled
            && first.TangentAlignment == second.TangentAlignment
            && first.AlignmentRadius == second.AlignmentRadius
            && first.CrossingPenalty == second.CrossingPenalty
            && first.HardBoundaryThreshold == second.HardBoundaryThreshold
            && first.TerminationStrength == second.TerminationStrength
            && first.InternalEdgeInfluence == second.InternalEdgeInfluence
            && first.TextureEdgeInfluence == second.TextureEdgeInfluence
            && first.ContourReinforcement == second.ContourReinforcement
            && first.CornerPreservation == second.CornerPreservation;
    }

    private static bool BackgroundSuppressionIsEquivalent(
        BackgroundSuppressionSettings first,
        BackgroundSuppressionSettings second)
    {
        return first.Enabled == second.Enabled
            && first.OverallStrength == second.OverallStrength
            && first.DetailFloor == second.DetailFloor
            && first.UncertaintyProtection == second.UncertaintyProtection
            && first.SilhouetteProtection == second.SilhouetteProtection
            && first.TransitionSoftness == second.TransitionSoftness
            && first.BackgroundPlacementWeight == second.BackgroundPlacementWeight
            && first.StrokeLengthMultiplier == second.StrokeLengthMultiplier
            && first.StrokeWidthMultiplier == second.StrokeWidthMultiplier
            && first.SegmentMultiplier == second.SegmentMultiplier
            && first.CurveFreedomMultiplier == second.CurveFreedomMultiplier
            && first.ColorSimplification == second.ColorSimplification;
    }
    private static bool RegionalSegmentationIsEquivalent(
        RegionSegmentationSettings first,
        RegionSegmentationSettings second)
    {
        return first.Enabled == second.Enabled
            && first.TargetRegionSize == second.TargetRegionSize
            && first.Compactness == second.Compactness
            && first.PreBlurSigma == second.PreBlurSigma
            && first.MaximumIterations == second.MaximumIterations
            && first.ConvergenceTolerance == second.ConvergenceTolerance;
    }

    private static bool RegionMergeIsEquivalent(
        RegionMergeSettings first,
        RegionMergeSettings second)
    {
        return first.IntermediateTargetRatio == second.IntermediateTargetRatio
            && first.BroadMassTargetRatio == second.BroadMassTargetRatio
            && first.IntermediateMaximumCost == second.IntermediateMaximumCost
            && first.BroadMassMaximumCost == second.BroadMassMaximumCost
            && first.StrongBoundaryThreshold == second.StrongBoundaryThreshold
            && first.MaximumParentAreaFraction == second.MaximumParentAreaFraction;
    }

}
