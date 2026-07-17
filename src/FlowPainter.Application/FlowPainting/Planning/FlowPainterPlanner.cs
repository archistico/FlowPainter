using FlowPainter.Application.Background;
using FlowPainter.Application.Boundaries;
using FlowPainter.Application.FlowPainting.Fields;
using FlowPainter.Application.Segmentation;
using FlowPainter.Application.Workloads;
using FlowPainter.Domain.Boundaries;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Randomness;
using FlowPainter.Domain.Strokes;

namespace FlowPainter.Application.FlowPainting.Planning;

public sealed class FlowPainterPlanner
{
    public const string PlannerVersion = "flow-field-v1";
    public const string DetailPlannerVersion = "flow-field-detail-v1";
    public const string BoundaryPlannerVersion = "flow-field-boundary-v1";
    public const string RegionalBoundaryPlannerVersion = "flow-field-regional-boundary-v1";
    public const string BackgroundPlannerVersion = "flow-field-background-v1";
    public const string RegionalBackgroundPlannerVersion = "flow-field-background-regional-boundary-v1";
    private const int ProgressBatchSize = 256;
    private const int CrossingSampleCount = 4;
    private readonly IFlowFieldFactory _fieldFactory;

    public FlowPainterPlanner(IFlowFieldFactory fieldFactory)
    {
        ArgumentNullException.ThrowIfNull(fieldFactory);
        _fieldFactory = fieldFactory;
    }

    public StrokePlan CreatePlan(
        IRgbaPixelSource source,
        StrokeDensityMap densityMap,
        ulong seed,
        FlowPainterSettings settings,
        IProgress<StrokePlanningProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return CreatePlanCore(
            source,
            densityMap,
            detailMap: null,
            guidanceField: null,
            artisticDetailField: null,
            seed,
            settings,
            PlannerVersion,
            progress,
            cancellationToken);
    }

    public StrokePlan CreatePlan(
        IRgbaPixelSource source,
        StrokeDensityMap densityMap,
        DetailMap detailMap,
        ulong seed,
        FlowPainterSettings settings,
        IProgress<StrokePlanningProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(detailMap);
        return CreatePlanCore(
            source,
            densityMap,
            detailMap,
            guidanceField: null,
            artisticDetailField: null,
            seed,
            settings,
            DetailPlannerVersion,
            progress,
            cancellationToken);
    }

    public StrokePlan CreatePlan(
        IRgbaPixelSource source,
        StrokeDensityMap densityMap,
        DetailMap detailMap,
        SceneBoundaryAnalysisResult boundaryAnalysis,
        ulong seed,
        FlowPainterSettings settings,
        IProgress<StrokePlanningProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(boundaryAnalysis);
        ArgumentNullException.ThrowIfNull(settings);
        if (!settings.BoundaryPainting.Enabled)
        {
            return CreatePlan(
                source,
                densityMap,
                detailMap,
                seed,
                settings,
                progress,
                cancellationToken);
        }

        BoundaryGuidanceField guidance = BoundaryGuidanceField.Create(
            boundaryAnalysis,
            settings.BoundaryPainting,
            cancellationToken);
        return CreatePlan(
            source,
            densityMap,
            detailMap,
            guidance,
            seed,
            settings,
            progress,
            cancellationToken);
    }

    public StrokePlan CreatePlan(
        IRgbaPixelSource source,
        StrokeDensityMap densityMap,
        DetailMap detailMap,
        SceneBoundaryAnalysisResult boundaryAnalysis,
        RegionSegmentationResult regionalSegmentation,
        ulong seed,
        FlowPainterSettings settings,
        IProgress<StrokePlanningProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(boundaryAnalysis);
        ArgumentNullException.ThrowIfNull(regionalSegmentation);
        ArgumentNullException.ThrowIfNull(settings);
        if (!settings.BoundaryPainting.Enabled)
        {
            return CreatePlan(
                source,
                densityMap,
                detailMap,
                seed,
                settings,
                progress,
                cancellationToken);
        }

        BoundaryGuidanceField guidance = BoundaryGuidanceField.Create(
            boundaryAnalysis,
            regionalSegmentation,
            settings.BoundaryPainting,
            cancellationToken);
        return CreateBoundaryGuidedPlan(
            source,
            densityMap,
            detailMap,
            guidance,
            artisticDetailField: null,
            seed,
            settings,
            RegionalBoundaryPlannerVersion,
            progress,
            cancellationToken);
    }

    public StrokePlan CreatePlan(
        IRgbaPixelSource source,
        StrokeDensityMap densityMap,
        DetailMap detailMap,
        BoundaryGuidanceField guidanceField,
        ulong seed,
        FlowPainterSettings settings,
        IProgress<StrokePlanningProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(detailMap);
        ArgumentNullException.ThrowIfNull(guidanceField);
        ArgumentNullException.ThrowIfNull(settings);
        if (!settings.BoundaryPainting.Enabled)
        {
            return CreatePlan(
                source,
                densityMap,
                detailMap,
                seed,
                settings,
                progress,
                cancellationToken);
        }

        return CreateBoundaryGuidedPlan(
            source,
            densityMap,
            detailMap,
            guidanceField,
            artisticDetailField: null,
            seed,
            settings,
            BoundaryPlannerVersion,
            progress,
            cancellationToken);
    }

    public StrokePlan CreateRegionalPlan(
        IRgbaPixelSource source,
        StrokeDensityMap densityMap,
        DetailMap detailMap,
        BoundaryGuidanceField guidanceField,
        ulong seed,
        FlowPainterSettings settings,
        IProgress<StrokePlanningProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(detailMap);
        ArgumentNullException.ThrowIfNull(guidanceField);
        ArgumentNullException.ThrowIfNull(settings);
        if (!settings.BoundaryPainting.Enabled)
        {
            return CreatePlan(
                source,
                densityMap,
                detailMap,
                seed,
                settings,
                progress,
                cancellationToken);
        }

        return CreateBoundaryGuidedPlan(
            source,
            densityMap,
            detailMap,
            guidanceField,
            artisticDetailField: null,
            seed,
            settings,
            RegionalBoundaryPlannerVersion,
            progress,
            cancellationToken);
    }

    public StrokePlan CreatePlan(
        IRgbaPixelSource source,
        StrokeDensityMap densityMap,
        BackgroundSuppressionResult backgroundSuppression,
        SceneBoundaryAnalysisResult boundaryAnalysis,
        ulong seed,
        FlowPainterSettings settings,
        IProgress<StrokePlanningProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backgroundSuppression);
        ArgumentNullException.ThrowIfNull(boundaryAnalysis);
        ArgumentNullException.ThrowIfNull(settings);
        if (!settings.BackgroundSuppression.Enabled)
        {
            return CreatePlan(
                source,
                densityMap,
                backgroundSuppression.EffectiveDetailMap,
                boundaryAnalysis,
                seed,
                settings,
                progress,
                cancellationToken);
        }

        BoundaryGuidanceField? guidance = settings.BoundaryPainting.Enabled
            ? BoundaryGuidanceField.Create(
                boundaryAnalysis,
                settings.BoundaryPainting,
                cancellationToken)
            : null;
        DetailMap effectiveDetailMap = guidance is null
            ? backgroundSuppression.EffectiveDetailMap
            : guidance.CreateReinforcedDetailMap(
                backgroundSuppression.EffectiveDetailMap,
                settings.BoundaryPainting.ContourReinforcement);
        return CreatePlanCore(
            source,
            densityMap,
            effectiveDetailMap,
            guidance,
            backgroundSuppression.ArtisticDetailField,
            seed,
            settings,
            BackgroundPlannerVersion,
            progress,
            cancellationToken);
    }

    public StrokePlan CreatePlan(
        IRgbaPixelSource source,
        StrokeDensityMap densityMap,
        BackgroundSuppressionResult backgroundSuppression,
        SceneBoundaryAnalysisResult boundaryAnalysis,
        RegionSegmentationResult regionalSegmentation,
        ulong seed,
        FlowPainterSettings settings,
        IProgress<StrokePlanningProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backgroundSuppression);
        ArgumentNullException.ThrowIfNull(boundaryAnalysis);
        ArgumentNullException.ThrowIfNull(regionalSegmentation);
        ArgumentNullException.ThrowIfNull(settings);
        if (!settings.BackgroundSuppression.Enabled)
        {
            return CreatePlan(
                source,
                densityMap,
                backgroundSuppression.EffectiveDetailMap,
                boundaryAnalysis,
                regionalSegmentation,
                seed,
                settings,
                progress,
                cancellationToken);
        }

        BoundaryGuidanceField? guidance = settings.BoundaryPainting.Enabled
            ? BoundaryGuidanceField.Create(
                boundaryAnalysis,
                regionalSegmentation,
                settings.BoundaryPainting,
                cancellationToken)
            : null;
        if (guidance is null)
        {
            return CreatePlanCore(
                source,
                densityMap,
                backgroundSuppression.EffectiveDetailMap,
                guidanceField: null,
                backgroundSuppression.ArtisticDetailField,
                seed,
                settings,
                BackgroundPlannerVersion,
                progress,
                cancellationToken);
        }

        return CreateBoundaryGuidedPlan(
            source,
            densityMap,
            backgroundSuppression.EffectiveDetailMap,
            guidance,
            backgroundSuppression.ArtisticDetailField,
            seed,
            settings,
            RegionalBackgroundPlannerVersion,
            progress,
            cancellationToken);
    }

    public StrokePlan CreateRegionalPlan(
        IRgbaPixelSource source,
        StrokeDensityMap densityMap,
        BackgroundSuppressionResult backgroundSuppression,
        BoundaryGuidanceField guidanceField,
        ulong seed,
        FlowPainterSettings settings,
        IProgress<StrokePlanningProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backgroundSuppression);
        ArgumentNullException.ThrowIfNull(guidanceField);
        ArgumentNullException.ThrowIfNull(settings);
        if (!settings.BackgroundSuppression.Enabled)
        {
            return CreateRegionalPlan(
                source,
                densityMap,
                backgroundSuppression.EffectiveDetailMap,
                guidanceField,
                seed,
                settings,
                progress,
                cancellationToken);
        }

        if (!settings.BoundaryPainting.Enabled)
        {
            return CreatePlanCore(
                source,
                densityMap,
                backgroundSuppression.EffectiveDetailMap,
                guidanceField: null,
                backgroundSuppression.ArtisticDetailField,
                seed,
                settings,
                BackgroundPlannerVersion,
                progress,
                cancellationToken);
        }

        return CreateBoundaryGuidedPlan(
            source,
            densityMap,
            backgroundSuppression.EffectiveDetailMap,
            guidanceField,
            backgroundSuppression.ArtisticDetailField,
            seed,
            settings,
            RegionalBackgroundPlannerVersion,
            progress,
            cancellationToken);
    }

    private StrokePlan CreateBoundaryGuidedPlan(
        IRgbaPixelSource source,
        StrokeDensityMap densityMap,
        DetailMap detailMap,
        BoundaryGuidanceField guidanceField,
        ArtisticDetailField? artisticDetailField,
        ulong seed,
        FlowPainterSettings settings,
        string plannerVersion,
        IProgress<StrokePlanningProgress>? progress,
        CancellationToken cancellationToken)
    {
        DetailMap reinforcedDetailMap = guidanceField.CreateReinforcedDetailMap(
            detailMap,
            settings.BoundaryPainting.ContourReinforcement);
        return CreatePlanCore(
            source,
            densityMap,
            reinforcedDetailMap,
            guidanceField,
            artisticDetailField,
            seed,
            settings,
            plannerVersion,
            progress,
            cancellationToken);
    }

    private StrokePlan CreatePlanCore(
        IRgbaPixelSource source,
        StrokeDensityMap densityMap,
        DetailMap? detailMap,
        BoundaryGuidanceField? guidanceField,
        ArtisticDetailField? artisticDetailField,
        ulong seed,
        FlowPainterSettings settings,
        string plannerVersion,
        IProgress<StrokePlanningProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(densityMap);
        ArgumentNullException.ThrowIfNull(settings);

        if (source.Size != densityMap.Size)
        {
            throw new ArgumentException(
                "The source image and stroke-density map must have identical dimensions.",
                nameof(densityMap));
        }

        if (detailMap is not null && source.Size != detailMap.Size)
        {
            throw new ArgumentException(
                "The source image and detail map must have identical dimensions.",
                nameof(detailMap));
        }

        if (guidanceField is not null && source.Size != guidanceField.Size)
        {
            throw new ArgumentException(
                "The source image and boundary-guidance field must have identical dimensions.",
                nameof(guidanceField));
        }

        if (artisticDetailField is not null && source.Size != artisticDetailField.Size)
        {
            throw new ArgumentException(
                "The source image and artistic-detail field must have identical dimensions.",
                nameof(artisticDetailField));
        }

        WorkloadBudgetPolicy.EnsureGenerationWithinBudget(
            GenerationWorkEstimator.EstimateFlow(settings));
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new StrokePlanningProgress(
            StrokePlanningStage.Preparing,
            0,
            settings.StrokeCount,
            0d));

        DeterministicRandom random = new(seed);
        int fieldSeed = random.NextInt32(int.MaxValue);
        IFlowField field = _fieldFactory.Create(fieldSeed, settings.Field);
        ArgumentNullException.ThrowIfNull(field);
        ArtisticDetailPointSampler? artisticSampler = artisticDetailField is null
            ? null
            : new ArtisticDetailPointSampler(
                artisticDetailField,
                settings.DetailInfluence,
                settings.BackgroundSuppression);
        DetailWeightedPointSampler? detailSampler = artisticSampler is not null
            || detailMap is null
            || settings.DetailInfluence.PlacementBias == 0d
                ? null
                : new DetailWeightedPointSampler(detailMap, settings.DetailInfluence);

        List<FlowStroke> strokes = new(settings.StrokeCount);

        for (int index = 0; index < settings.StrokeCount; index++)
        {
            if (index % ProgressBatchSize == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new StrokePlanningProgress(
                    StrokePlanningStage.PlanningStrokes,
                    index,
                    settings.StrokeCount,
                    CalculateFraction(index, settings.StrokeCount)));
            }

            NormalizedPoint start = artisticSampler is not null
                ? artisticSampler.Sample(random)
                : detailSampler is null
                    ? new NormalizedPoint(random.NextDouble(), random.NextDouble())
                    : detailSampler.Sample(random);
            double localDetail = detailMap?.SampleNearest(start) ?? 0d;
            double artisticDetail = artisticDetailField?.SampleNearest(start) ?? 0d;
            double localSuppression = Math.Max(0d, -artisticDetail);
            double density = densityMap.SampleNearest(start);
            double lengthMultiplier = detailMap is null
                ? 1d
                : settings.DetailInfluence.GetLengthMultiplier(localDetail);
            lengthMultiplier *= settings.BackgroundSuppression.GetStrokeLengthMultiplier(localSuppression);
            double maximumLength = settings.LengthScale * density * lengthMultiplier;
            int localSegmentCount = Math.Max(
                2,
                checked((int)Math.Round(
                    settings.SegmentCount
                    * settings.BackgroundSuppression.GetSegmentMultiplier(localSuppression),
                    MidpointRounding.AwayFromZero)));
            double localMaximumCurve = Math.Min(
                AngleMath.Tau,
                settings.MaximumCurveRadians
                * settings.BackgroundSuppression.GetCurveFreedomMultiplier(localSuppression));
            List<RelativePoint> points = guidanceField is null
                ? CreateUnconstrainedPath(
                    start,
                    maximumLength,
                    localSegmentCount,
                    localMaximumCurve,
                    field)
                : CreateBoundaryAwarePath(
                    start,
                    maximumLength,
                    localSegmentCount,
                    localMaximumCurve,
                    settings,
                    field,
                    guidanceField);
            Rgba32 sampledColor = source.SampleNearest(start);
            sampledColor = SimplifyColor(
                sampledColor,
                localSuppression * settings.BackgroundSuppression.ColorSimplification);
            Rgba32 strokeColor = ApplyOpacity(sampledColor, settings.StrokeOpacity);
            double widthPixels = settings.MinimumStrokeWidthPixels
                + (random.NextDouble()
                    * (settings.MaximumStrokeWidthPixels
                        - settings.MinimumStrokeWidthPixels));

            if (detailMap is not null)
            {
                widthPixels *= settings.DetailInfluence.GetWidthMultiplier(localDetail);
            }

            widthPixels *= settings.BackgroundSuppression.GetStrokeWidthMultiplier(localSuppression);

            double widthRelativeToReference = widthPixels / settings.ReferenceMaximumDimension;

            strokes.Add(new FlowStroke(index, points, strokeColor, widthRelativeToReference));
        }

        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new StrokePlanningProgress(
            StrokePlanningStage.Completed,
            settings.StrokeCount,
            settings.StrokeCount,
            1d));

        return new StrokePlan(
            source.Size,
            seed,
            fieldSeed,
            settings.ReferenceMaximumDimension,
            strokes,
            settings.BackgroundMode,
            plannerVersion);
    }

    private static List<RelativePoint> CreateUnconstrainedPath(
        NormalizedPoint start,
        double maximumLength,
        int segmentCount,
        double maximumCurveRadians,
        IFlowField field)
    {
        double segmentLength = maximumLength / segmentCount;
        double x = start.X;
        double y = start.Y;
        double startAngle = 0d;

        List<RelativePoint> points = new(segmentCount + 1)
        {
            new RelativePoint(x, y)
        };

        for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
        {
            double angle = field.SampleAngle(x, y);
            if (!double.IsFinite(angle))
            {
                throw new InvalidOperationException("The flow field returned a non-finite angle.");
            }

            angle = AngleMath.NormalizeRadians(angle);
            if (segmentIndex == 0)
            {
                startAngle = angle;
            }
            else if (AngleMath.ShortestDistanceRadians(startAngle, angle) > maximumCurveRadians)
            {
                break;
            }

            double nextX = x + (Math.Cos(angle) * segmentLength);
            double nextY = y - (Math.Sin(angle) * segmentLength);
            bool outside = nextX < 0d || nextX > 1d || nextY < 0d || nextY > 1d;

            x = Math.Clamp(nextX, 0d, 1d);
            y = Math.Clamp(nextY, 0d, 1d);
            points.Add(new RelativePoint(x, y));

            if (outside)
            {
                break;
            }
        }

        return points;
    }

    private static List<RelativePoint> CreateBoundaryAwarePath(
        NormalizedPoint start,
        double maximumLength,
        int segmentCount,
        double maximumCurveRadians,
        FlowPainterSettings settings,
        IFlowField field,
        BoundaryGuidanceField guidanceField)
    {
        BoundaryPaintingSettings boundarySettings = settings.BoundaryPainting;
        double baseSegmentLength = maximumLength / segmentCount;
        double x = start.X;
        double y = start.Y;
        double startAngle = 0d;

        List<RelativePoint> points = new(segmentCount + 1)
        {
            new RelativePoint(x, y)
        };

        for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
        {
            NormalizedPoint currentPoint = new(x, y);
            double baseAngle = field.SampleAngle(x, y);
            if (!double.IsFinite(baseAngle))
            {
                throw new InvalidOperationException("The flow field returned a non-finite angle.");
            }

            baseAngle = AngleMath.NormalizeRadians(baseAngle);
            BoundaryGuidanceSample currentGuidance = guidanceField.SampleNearest(currentPoint);
            double angle = AlignToBoundary(
                baseAngle,
                currentGuidance,
                boundarySettings.TangentAlignment * currentGuidance.Influence);
            double segmentLength = baseSegmentLength * Math.Clamp(
                1d - (0.72d * boundarySettings.CornerPreservation * currentGuidance.CornerStrength),
                0.2d,
                1d);

            if (segmentIndex == 0)
            {
                startAngle = angle;
            }
            else if (AngleMath.ShortestDistanceRadians(startAngle, angle) > maximumCurveRadians)
            {
                break;
            }

            CandidateStep candidate = CreateCandidate(x, y, angle, segmentLength);
            CrossingEvaluation crossing = EvaluateCrossing(
                x,
                y,
                candidate.X,
                candidate.Y,
                guidanceField);

            if (crossing.Risk > 0d && crossing.Guidance.HasDirection)
            {
                angle = AlignToBoundary(
                    angle,
                    crossing.Guidance,
                    boundarySettings.CrossingPenalty * crossing.Risk);
                candidate = CreateCandidate(x, y, angle, segmentLength);
                crossing = EvaluateCrossing(
                    x,
                    y,
                    candidate.X,
                    candidate.Y,
                    guidanceField);
            }

            bool hardStop = crossing.Risk * boundarySettings.TerminationStrength
                >= boundarySettings.HardBoundaryThreshold;
            if (hardStop && points.Count > 1)
            {
                break;
            }

            x = candidate.X;
            y = candidate.Y;
            points.Add(new RelativePoint(x, y));

            if (candidate.Outside)
            {
                break;
            }
        }

        return points;
    }

    private static CandidateStep CreateCandidate(
        double x,
        double y,
        double angle,
        double segmentLength)
    {
        double nextX = x + (Math.Cos(angle) * segmentLength);
        double nextY = y - (Math.Sin(angle) * segmentLength);
        bool outside = nextX < 0d || nextX > 1d || nextY < 0d || nextY > 1d;
        return new CandidateStep(
            Math.Clamp(nextX, 0d, 1d),
            Math.Clamp(nextY, 0d, 1d),
            outside);
    }

    private static CrossingEvaluation EvaluateCrossing(
        double startX,
        double startY,
        double endX,
        double endY,
        BoundaryGuidanceField guidanceField)
    {
        double movementX = endX - startX;
        double movementY = endY - startY;
        double movementLength = Math.Sqrt((movementX * movementX) + (movementY * movementY));
        if (movementLength <= double.Epsilon)
        {
            return default;
        }

        movementX /= movementLength;
        movementY /= movementLength;
        double maximumRisk = 0d;
        BoundaryGuidanceSample strongestGuidance = default;

        for (int sampleIndex = 1; sampleIndex <= CrossingSampleCount; sampleIndex++)
        {
            double fraction = sampleIndex / (double)CrossingSampleCount;
            NormalizedPoint point = new(
                startX + ((endX - startX) * fraction),
                startY + ((endY - startY) * fraction));
            BoundaryGuidanceSample guidance = guidanceField.SampleNearest(point);
            if (!guidance.HasDirection)
            {
                continue;
            }

            double normalMovement = Math.Abs(
                (movementX * guidance.Tangent.Y)
                - (movementY * guidance.Tangent.X));
            double risk = guidance.Hardness * normalMovement;
            if (risk <= maximumRisk)
            {
                continue;
            }

            maximumRisk = risk;
            strongestGuidance = guidance;
        }

        return new CrossingEvaluation(maximumRisk, strongestGuidance);
    }

    private static double AlignToBoundary(
        double sourceAngle,
        BoundaryGuidanceSample guidance,
        double amount)
    {
        if (!guidance.HasDirection || amount <= 0d)
        {
            return sourceAngle;
        }

        double tangentAngle = AngleMath.NormalizeRadians(
            Math.Atan2(-guidance.Tangent.Y, guidance.Tangent.X));
        double reverseTangent = AngleMath.NormalizeRadians(tangentAngle + Math.PI);
        double targetAngle = AngleMath.ShortestDistanceRadians(sourceAngle, tangentAngle)
            <= AngleMath.ShortestDistanceRadians(sourceAngle, reverseTangent)
                ? tangentAngle
                : reverseTangent;
        double blend = Math.Clamp(amount, 0d, 1d);
        double x = ((1d - blend) * Math.Cos(sourceAngle)) + (blend * Math.Cos(targetAngle));
        double y = ((1d - blend) * Math.Sin(sourceAngle)) + (blend * Math.Sin(targetAngle));
        if (Math.Abs(x) <= double.Epsilon && Math.Abs(y) <= double.Epsilon)
        {
            return targetAngle;
        }

        return AngleMath.NormalizeRadians(Math.Atan2(y, x));
    }

    private static Rgba32 SimplifyColor(Rgba32 color, double amount)
    {
        if (amount <= 0d)
        {
            return color;
        }

        int levels = Math.Max(
            2,
            checked((int)Math.Round(256d - (224d * Math.Clamp(amount, 0d, 1d)), MidpointRounding.AwayFromZero)));
        double step = 255d / (levels - 1d);
        return new Rgba32(
            Quantize(color.Red, step),
            Quantize(color.Green, step),
            Quantize(color.Blue, step),
            color.Alpha);
    }

    private static byte Quantize(byte value, double step)
    {
        return checked((byte)Math.Round(
            Math.Clamp(Math.Round(value / step, MidpointRounding.AwayFromZero) * step, 0d, byte.MaxValue),
            MidpointRounding.AwayFromZero));
    }

    private static Rgba32 ApplyOpacity(Rgba32 color, double opacity)
    {
        byte alpha = checked((byte)Math.Round(
            color.Alpha * opacity,
            MidpointRounding.AwayFromZero));
        return new Rgba32(color.Red, color.Green, color.Blue, alpha);
    }

    private static double CalculateFraction(int completed, int total)
    {
        return total == 0 ? 0d : 0.02d + (0.96d * completed / total);
    }

    private readonly record struct CandidateStep(double X, double Y, bool Outside);

    private readonly record struct CrossingEvaluation(
        double Risk,
        BoundaryGuidanceSample Guidance);
}
