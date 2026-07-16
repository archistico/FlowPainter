using FlowPainter.Application.Background;
using FlowPainter.Application.Boundaries;
using FlowPainter.Application.FlowPainting.Fields;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.PrimitiveGeneration;
using FlowPainter.Application.Workloads;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Hybrid;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Primitives;
using FlowPainter.Domain.Strokes;

namespace FlowPainter.Application.Hybrid;

public sealed class HybridPlanComposer
{
    private const ulong PrimitiveSeedSalt = 0xD1B5_4A32_D192_ED03UL;
    private const ulong FlowSeedSalt = 0x94D0_49BB_1331_11EBUL;
    private const ulong RefinementSeedSalt = 0xBF58_476D_1CE4_E5B9UL;
    private readonly PrimitivePlanOptimizer _primitiveOptimizer;
    private readonly IFlowFieldFactory _baseFieldFactory;

    public HybridPlanComposer()
        : this(new PrimitivePlanOptimizer(), new DefaultFlowFieldFactory())
    {
    }

    public HybridPlanComposer(
        PrimitivePlanOptimizer primitiveOptimizer,
        IFlowFieldFactory baseFieldFactory)
    {
        ArgumentNullException.ThrowIfNull(primitiveOptimizer);
        ArgumentNullException.ThrowIfNull(baseFieldFactory);
        _primitiveOptimizer = primitiveOptimizer;
        _baseFieldFactory = baseFieldFactory;
    }

    public HybridPlan CreatePlan(
        IRgbaPixelSource source,
        StrokeDensityMap densityMap,
        DetailMap detailMap,
        ulong seed,
        FlowPainterSettings flowSettings,
        PrimitiveGenerationSettings primitiveSettings,
        HybridGenerationSettings hybridSettings,
        IProgress<HybridPlanningProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return CreatePlanCore(
            source,
            densityMap,
            detailMap,
            boundaryAnalysis: null,
            backgroundSuppression: null,
            seed,
            flowSettings,
            primitiveSettings,
            hybridSettings,
            progress,
            cancellationToken);
    }

    public HybridPlan CreatePlan(
        IRgbaPixelSource source,
        StrokeDensityMap densityMap,
        DetailMap detailMap,
        SceneBoundaryAnalysisResult boundaryAnalysis,
        ulong seed,
        FlowPainterSettings flowSettings,
        PrimitiveGenerationSettings primitiveSettings,
        HybridGenerationSettings hybridSettings,
        IProgress<HybridPlanningProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(boundaryAnalysis);
        return CreatePlanCore(
            source,
            densityMap,
            detailMap,
            boundaryAnalysis,
            backgroundSuppression: null,
            seed,
            flowSettings,
            primitiveSettings,
            hybridSettings,
            progress,
            cancellationToken);
    }

    public HybridPlan CreatePlan(
        IRgbaPixelSource source,
        StrokeDensityMap densityMap,
        BackgroundSuppressionResult backgroundSuppression,
        SceneBoundaryAnalysisResult boundaryAnalysis,
        ulong seed,
        FlowPainterSettings flowSettings,
        PrimitiveGenerationSettings primitiveSettings,
        HybridGenerationSettings hybridSettings,
        IProgress<HybridPlanningProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backgroundSuppression);
        ArgumentNullException.ThrowIfNull(boundaryAnalysis);
        return CreatePlanCore(
            source,
            densityMap,
            backgroundSuppression.EffectiveDetailMap,
            boundaryAnalysis,
            backgroundSuppression,
            seed,
            flowSettings,
            primitiveSettings,
            hybridSettings,
            progress,
            cancellationToken);
    }

    private HybridPlan CreatePlanCore(
        IRgbaPixelSource source,
        StrokeDensityMap densityMap,
        DetailMap detailMap,
        SceneBoundaryAnalysisResult? boundaryAnalysis,
        BackgroundSuppressionResult? backgroundSuppression,
        ulong seed,
        FlowPainterSettings flowSettings,
        PrimitiveGenerationSettings primitiveSettings,
        HybridGenerationSettings hybridSettings,
        IProgress<HybridPlanningProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(densityMap);
        ArgumentNullException.ThrowIfNull(detailMap);
        ArgumentNullException.ThrowIfNull(flowSettings);
        ArgumentNullException.ThrowIfNull(primitiveSettings);
        ArgumentNullException.ThrowIfNull(hybridSettings);
        if (source.Size != densityMap.Size || source.Size != detailMap.Size)
        {
            throw new ArgumentException("The source, density map and detail map must have identical dimensions.");
        }

        WorkloadBudgetPolicy.EnsureGenerationWithinBudget(
            GenerationWorkEstimator.EstimateHybrid(
                source.Size,
                flowSettings,
                primitiveSettings,
                hybridSettings));

        if (flowSettings.BoundaryPainting.Enabled && boundaryAnalysis is null)
        {
            throw new ArgumentNullException(
                nameof(boundaryAnalysis),
                "Boundary analysis is required when boundary-aware painting is enabled.");
        }

        BoundaryGuidanceField? boundaryGuidance = boundaryAnalysis is null
            || !flowSettings.BoundaryPainting.Enabled
                ? null
                : BoundaryGuidanceField.Create(
                    boundaryAnalysis,
                    flowSettings.BoundaryPainting,
                    cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new HybridPlanningProgress(HybridPlanningStage.Preparing, 0d, "Preparing hybrid plan."));

        PrimitiveGenerationSettings scaledPrimitiveSettings = CopyPrimitiveSettings(
            primitiveSettings,
            ScaleCount(primitiveSettings.PrimitiveCount, hybridSettings.PrimitiveBudgetFraction));
        ForwardingProgress<PrimitiveGenerationProgress>? primitiveProgress = progress is null
            ? null
            : new ForwardingProgress<PrimitiveGenerationProgress>(value => progress.Report(
                new HybridPlanningProgress(
                    HybridPlanningStage.GeneratingPrimitives,
                    value.Fraction * 0.45d,
                    $"Generating primitive layer {value.CompletedPrimitives:N0} / {value.RequestedPrimitives:N0}.")));
        PrimitivePlan primitivePlan = _primitiveOptimizer.CreatePlan(
            source,
            detailMap,
            seed ^ PrimitiveSeedSalt,
            scaledPrimitiveSettings,
            primitiveProgress,
            cancellationToken);

        PrimitiveInfluenceFlowFieldFactory influencedFactory = new(
            _baseFieldFactory,
            primitivePlan,
            hybridSettings);
        FlowPainterPlanner flowPlanner = new(influencedFactory);
        FlowPainterSettings baseFlowSettings = CopyFlowSettings(
            flowSettings,
            ScaleCount(flowSettings.StrokeCount, hybridSettings.FlowBudgetFraction),
            flowSettings.LengthScale,
            flowSettings.MinimumStrokeWidthPixels,
            flowSettings.MaximumStrokeWidthPixels,
            flowSettings.DetailInfluence);
        ForwardingProgress<StrokePlanningProgress>? flowProgress = progress is null
            ? null
            : new ForwardingProgress<StrokePlanningProgress>(value => progress.Report(
                new HybridPlanningProgress(
                    HybridPlanningStage.PlanningFlowStrokes,
                    0.45d + (value.Fraction * 0.30d),
                    $"Planning primitive-guided strokes {value.CompletedStrokes:N0} / {value.TotalStrokes:N0}.")));
        StrokePlan flowPlan = backgroundSuppression is not null
            && flowSettings.BackgroundSuppression.Enabled
                ? flowPlanner.CreatePlan(
                    source,
                    densityMap,
                    backgroundSuppression,
                    boundaryAnalysis!,
                    seed ^ FlowSeedSalt,
                    baseFlowSettings,
                    flowProgress,
                    cancellationToken)
                : boundaryGuidance is null
                    ? flowPlanner.CreatePlan(
                        source,
                        densityMap,
                        detailMap,
                        seed ^ FlowSeedSalt,
                        baseFlowSettings,
                        flowProgress,
                        cancellationToken)
                    : flowPlanner.CreatePlan(
                        source,
                        densityMap,
                        detailMap,
                        boundaryGuidance,
                        seed ^ FlowSeedSalt,
                        baseFlowSettings,
                        flowProgress,
                        cancellationToken);

        DetailInfluenceSettings refinementDetail = new(
            Math.Min(20d, flowSettings.DetailInfluence.PlacementBias + hybridSettings.RefinementDetailBias),
            flowSettings.DetailInfluence.DetailedLengthMultiplier,
            flowSettings.DetailInfluence.BackgroundLengthMultiplier,
            flowSettings.DetailInfluence.DetailedWidthMultiplier,
            flowSettings.DetailInfluence.BackgroundWidthMultiplier,
            flowSettings.DetailInfluence.RegionTransitionWidth);
        FlowPainterSettings refinementSettings = CopyFlowSettings(
            flowSettings,
            ScaleCount(flowSettings.StrokeCount, hybridSettings.RefinementBudgetFraction),
            flowSettings.LengthScale * hybridSettings.RefinementLengthMultiplier,
            flowSettings.MinimumStrokeWidthPixels * hybridSettings.RefinementWidthMultiplier,
            flowSettings.MaximumStrokeWidthPixels * hybridSettings.RefinementWidthMultiplier,
            refinementDetail);
        ForwardingProgress<StrokePlanningProgress>? refinementProgress = progress is null
            ? null
            : new ForwardingProgress<StrokePlanningProgress>(value => progress.Report(
                new HybridPlanningProgress(
                    HybridPlanningStage.PlanningRefinementStrokes,
                    0.75d + (value.Fraction * 0.25d),
                    $"Planning detail refinement {value.CompletedStrokes:N0} / {value.TotalStrokes:N0}.")));
        StrokePlan refinementPlan = backgroundSuppression is not null
            && flowSettings.BackgroundSuppression.Enabled
                ? flowPlanner.CreatePlan(
                    source,
                    densityMap,
                    backgroundSuppression,
                    boundaryAnalysis!,
                    seed ^ RefinementSeedSalt,
                    refinementSettings,
                    refinementProgress,
                    cancellationToken)
                : boundaryGuidance is null
                    ? flowPlanner.CreatePlan(
                        source,
                        densityMap,
                        detailMap,
                        seed ^ RefinementSeedSalt,
                        refinementSettings,
                        refinementProgress,
                        cancellationToken)
                    : flowPlanner.CreatePlan(
                        source,
                        densityMap,
                        detailMap,
                        boundaryGuidance,
                        seed ^ RefinementSeedSalt,
                        refinementSettings,
                        refinementProgress,
                        cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new HybridPlanningProgress(HybridPlanningStage.Completed, 1d, "Hybrid plan completed."));
        return new HybridPlan(seed, primitivePlan, flowPlan, refinementPlan);
    }

    private static int ScaleCount(int total, double fraction)
    {
        return Math.Max(1, checked((int)Math.Round(total * fraction, MidpointRounding.AwayFromZero)));
    }

    private static PrimitiveGenerationSettings CopyPrimitiveSettings(
        PrimitiveGenerationSettings source,
        int primitiveCount)
    {
        return new PrimitiveGenerationSettings(
            primitiveCount,
            source.CandidatesPerStep,
            source.MutationIterations,
            source.MinimumSize,
            source.MaximumSize,
            source.Opacity,
            source.DetailSizeInfluence,
            source.DetailPlacementBias,
            source.DetailErrorWeight,
            source.DetailSearchInfluence,
            source.AllowedKinds);
    }

    private static FlowPainterSettings CopyFlowSettings(
        FlowPainterSettings source,
        int strokeCount,
        double lengthScale,
        double minimumWidth,
        double maximumWidth,
        DetailInfluenceSettings detailInfluence)
    {
        return new FlowPainterSettings(
            source.Field,
            strokeCount,
            source.SegmentCount,
            source.ReferenceMaximumDimension,
            source.UniformDensity,
            lengthScale,
            source.MaximumCurveRadians,
            minimumWidth,
            maximumWidth,
            source.StrokeOpacity,
            StrokePlanBackgroundMode.SourceImage,
            source.DetailAnalysis,
            detailInfluence,
            source.Brush,
            source.SemanticAnalysis,
            source.BoundaryAnalysis,
            source.BoundaryPainting,
            source.BackgroundSuppression);
    }
    private sealed class ForwardingProgress<T> : IProgress<T>
    {
        private readonly Action<T> _report;

        public ForwardingProgress(Action<T> report)
        {
            ArgumentNullException.ThrowIfNull(report);
            _report = report;
        }

        public void Report(T value)
        {
            _report(value);
        }
    }

}
