using FlowPainter.Application.FlowPainting.Fields;
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
    private const int ProgressBatchSize = 256;
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
            seed,
            settings,
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
            seed,
            settings,
            progress,
            cancellationToken);
    }

    private StrokePlan CreatePlanCore(
        IRgbaPixelSource source,
        StrokeDensityMap densityMap,
        DetailMap? detailMap,
        ulong seed,
        FlowPainterSettings settings,
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
        DetailWeightedPointSampler? detailSampler = detailMap is null
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

            NormalizedPoint start = detailSampler is null
                ? new NormalizedPoint(random.NextDouble(), random.NextDouble())
                : detailSampler.Sample(random);
            double localDetail = detailMap?.SampleNearest(start) ?? 0d;
            double density = densityMap.SampleNearest(start);
            double lengthMultiplier = detailMap is null
                ? 1d
                : settings.DetailInfluence.GetLengthMultiplier(localDetail);
            double maximumLength = settings.LengthScale * density * lengthMultiplier;
            List<RelativePoint> points = CreatePath(start, maximumLength, settings, field);
            Rgba32 sampledColor = source.SampleNearest(start);
            Rgba32 strokeColor = ApplyOpacity(sampledColor, settings.StrokeOpacity);
            double widthPixels = settings.MinimumStrokeWidthPixels
                + (random.NextDouble()
                    * (settings.MaximumStrokeWidthPixels
                        - settings.MinimumStrokeWidthPixels));

            if (detailMap is not null)
            {
                widthPixels *= settings.DetailInfluence.GetWidthMultiplier(localDetail);
            }

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
            detailMap is null ? PlannerVersion : DetailPlannerVersion);
    }

    private static List<RelativePoint> CreatePath(
        NormalizedPoint start,
        double maximumLength,
        FlowPainterSettings settings,
        IFlowField field)
    {
        double segmentLength = maximumLength / settings.SegmentCount;
        double x = start.X;
        double y = start.Y;
        double startAngle = 0d;

        List<RelativePoint> points = new(settings.SegmentCount + 1)
        {
            new RelativePoint(x, y)
        };

        for (int segmentIndex = 0; segmentIndex < settings.SegmentCount; segmentIndex++)
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
            else if (AngleMath.ShortestDistanceRadians(startAngle, angle) > settings.MaximumCurveRadians)
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
}
