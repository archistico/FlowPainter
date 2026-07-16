using System.Diagnostics.CodeAnalysis;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Primitives;
using FlowPainter.Domain.Randomness;

namespace FlowPainter.Application.PrimitiveGeneration;

public sealed class DefaultPrimitiveCandidateFactory : IPrimitiveCandidateFactory
{
    private static readonly PrimitiveKind[] OrderedKinds =
    [
        PrimitiveKind.Triangle,
        PrimitiveKind.Rectangle,
        PrimitiveKind.RotatedRectangle,
        PrimitiveKind.Circle,
        PrimitiveKind.Ellipse
    ];

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This implementation participates in a replaceable application-service contract and intentionally retains instance semantics.")]
    public GeometricPrimitive Create(
        int index,
        DetailMap? detailMap,
        PrimitiveGenerationSettings settings,
        IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(random);
        if (detailMap is not null && settings.DetailPlacementBias > 0d)
        {
            return CreateAtPoint(index, SampleDetailWeightedPoint(detailMap, settings, random), detailMap, settings, random);
        }

        NormalizedPoint center = new(random.NextDouble(), random.NextDouble());
        return CreateAtPoint(index, center, detailMap, settings, random);
    }

    private static GeometricPrimitive CreateAtPoint(
        int index,
        NormalizedPoint center,
        DetailMap? detailMap,
        PrimitiveGenerationSettings settings,
        IRandomSource random)
    {
        PrimitiveKind kind = SelectKind(settings.AllowedKinds, random);
        double localDetail = detailMap?.SampleNearest(center) ?? 0d;
        double detailAdjustedMaximum = settings.MaximumSize
            - ((settings.MaximumSize - settings.MinimumSize)
                * localDetail
                * settings.DetailSizeInfluence);
        double width = RandomSize(settings.MinimumSize, detailAdjustedMaximum, random);
        double height = RandomSize(settings.MinimumSize, detailAdjustedMaximum, random);
        double rotation = random.NextDouble() * AngleMath.Tau;

        if (kind == PrimitiveKind.Circle)
        {
            height = width;
        }
        else if (kind == PrimitiveKind.Rectangle)
        {
            rotation = 0d;
        }

        return new GeometricPrimitive(
            index,
            kind,
            center,
            width,
            height,
            rotation,
            Rgba32.Opaque(0, 0, 0));
    }

    private static NormalizedPoint SampleDetailWeightedPoint(
        DetailMap detailMap,
        PrimitiveGenerationSettings settings,
        IRandomSource random)
    {
        double maximumWeight = 1d + settings.DetailPlacementBias;
        for (int attempt = 0; attempt < 64; attempt++)
        {
            NormalizedPoint point = new(random.NextDouble(), random.NextDouble());
            double weight = 1d + (settings.DetailPlacementBias * detailMap.SampleNearest(point));
            if (random.NextDouble() * maximumWeight <= weight)
            {
                return point;
            }
        }

        return new NormalizedPoint(random.NextDouble(), random.NextDouble());
    }

    private static PrimitiveKind SelectKind(PrimitiveKindSet allowedKinds, IRandomSource random)
    {
        Span<PrimitiveKind> available = stackalloc PrimitiveKind[OrderedKinds.Length];
        int count = 0;
        foreach (PrimitiveKind kind in OrderedKinds)
        {
            PrimitiveKindSet flag = ToFlag(kind);
            if ((allowedKinds & flag) != 0)
            {
                available[count++] = kind;
            }
        }

        return available[random.NextInt32(count)];
    }

    private static PrimitiveKindSet ToFlag(PrimitiveKind kind)
    {
        return kind switch
        {
            PrimitiveKind.Triangle => PrimitiveKindSet.Triangle,
            PrimitiveKind.Rectangle => PrimitiveKindSet.Rectangle,
            PrimitiveKind.RotatedRectangle => PrimitiveKindSet.RotatedRectangle,
            PrimitiveKind.Circle => PrimitiveKindSet.Circle,
            PrimitiveKind.Ellipse => PrimitiveKindSet.Ellipse,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown primitive kind.")
        };
    }

    private static double RandomSize(double minimum, double maximum, IRandomSource random)
    {
        if (maximum <= minimum)
        {
            return minimum;
        }

        return minimum + ((maximum - minimum) * random.NextDouble());
    }
}
