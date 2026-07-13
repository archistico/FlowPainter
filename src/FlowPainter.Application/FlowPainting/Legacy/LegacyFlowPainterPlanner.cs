using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Randomness;
using FlowPainter.Domain.Strokes;

namespace FlowPainter.Application.FlowPainting.Legacy;

public sealed class LegacyFlowPainterPlanner
{
    private readonly ILegacyScalarFieldFactory _fieldFactory;

    public LegacyFlowPainterPlanner(ILegacyScalarFieldFactory fieldFactory)
    {
        ArgumentNullException.ThrowIfNull(fieldFactory);
        _fieldFactory = fieldFactory;
    }

    public StrokePlan CreatePlan(
        IRgbaPixelSource source,
        LegacyDensityMap densityMap,
        ulong seed,
        LegacyFlowPainterSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(densityMap);

        if (source.Size != densityMap.Size)
        {
            throw new ArgumentException(
                "The source image and legacy density map must have identical dimensions.",
                nameof(densityMap));
        }

        LegacyFlowPainterSettings effectiveSettings = settings ?? new LegacyFlowPainterSettings();
        DeterministicRandom random = new(seed);
        int fieldSeed = random.NextInt32(int.MaxValue);
        ILegacyScalarField field = _fieldFactory.Create(fieldSeed);
        ArgumentNullException.ThrowIfNull(field);

        List<FlowStroke> strokes = new(effectiveSettings.StrokeCount);

        for (int index = 0; index < effectiveSettings.StrokeCount; index++)
        {
            NormalizedPoint start = new(random.NextDouble(), random.NextDouble());
            double density = densityMap.SampleNearest(start);
            double maximumLength = effectiveSettings.LengthScale * density;

            List<RelativePoint> points = CreatePath(
                start,
                maximumLength,
                effectiveSettings,
                field);

            // The original implementation sampled the field once more after
            // constructing the path, but did not use the value. The field is
            // required to be a pure coordinate function; the call is retained
            // as part of the characterization sequence.
            _ = field.Sample(start.X * effectiveSettings.NoiseScale, start.Y * effectiveSettings.NoiseScale);

            Rgba32 color = source.SampleNearest(start);

            // The original implementation computed darkness but never used it.
            // Consuming the value preserves the legacy random sequence.
            _ = random.NextDouble();

            double widthPixels = effectiveSettings.MinimumStrokeWidthPixels
                + (random.NextDouble()
                    * (effectiveSettings.MaximumStrokeWidthPixels
                        - effectiveSettings.MinimumStrokeWidthPixels));

            double widthRelativeToReference = widthPixels / effectiveSettings.ReferenceMaximumDimension;

            strokes.Add(new FlowStroke(index, points, color, widthRelativeToReference));
        }

        return new StrokePlan(
            source.Size,
            seed,
            fieldSeed,
            effectiveSettings.ReferenceMaximumDimension,
            strokes);
    }

    private static List<RelativePoint> CreatePath(
        NormalizedPoint start,
        double maximumLength,
        LegacyFlowPainterSettings settings,
        ILegacyScalarField field)
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
            double noise = field.Sample(x * settings.NoiseScale, y * settings.NoiseScale);
            if (!double.IsFinite(noise))
            {
                throw new InvalidOperationException("The legacy scalar field returned a non-finite value.");
            }

            double angle = noise * AngleMath.Tau;

            if (segmentIndex == 0)
            {
                startAngle = angle;
            }
            else if (AngleMath.ShortestDistanceRadians(startAngle, angle) > settings.MaximumCurveRadians)
            {
                break;
            }

            x += Math.Cos(angle) * segmentLength;
            y -= Math.Sin(angle) * segmentLength;
            points.Add(new RelativePoint(x, y));
        }

        return points;
    }
}
