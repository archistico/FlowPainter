using FlowPainter.Application.FlowPainting.Fields;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Hybrid;
using FlowPainter.Domain.Primitives;

namespace FlowPainter.Application.Hybrid;

public sealed class PrimitiveInfluenceFlowField : IFlowField
{
    private readonly IFlowField _baseField;
    private readonly PrimitivePlan _primitivePlan;
    private readonly HybridGenerationSettings _settings;

    public PrimitiveInfluenceFlowField(
        IFlowField baseField,
        PrimitivePlan primitivePlan,
        HybridGenerationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(baseField);
        ArgumentNullException.ThrowIfNull(primitivePlan);
        ArgumentNullException.ThrowIfNull(settings);
        _baseField = baseField;
        _primitivePlan = primitivePlan;
        _settings = settings;
    }

    public double SampleAngle(double x, double y)
    {
        ValidateCoordinate(x, nameof(x));
        ValidateCoordinate(y, nameof(y));

        double baseAngle = _baseField.SampleAngle(x, y);
        if (_settings.InfluenceStrength == 0d || _primitivePlan.Primitives.Count == 0)
        {
            return baseAngle;
        }

        List<WeightedAngle> influences = [];
        foreach (GeometricPrimitive primitive in _primitivePlan.Primitives)
        {
            double deltaX = x - primitive.Center.X;
            double deltaY = y - primitive.Center.Y;
            double radius = Math.Max(primitive.Width, primitive.Height)
                * _settings.InfluenceRadiusMultiplier;
            double distance = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
            if (distance >= radius)
            {
                continue;
            }

            double normalizedDistance = distance / Math.Max(radius, double.Epsilon);
            double falloff = 1d - normalizedDistance;
            double weight = _settings.InfluenceStrength * falloff * falloff;
            double angle = CalculateInfluenceAngle(primitive, deltaX, deltaY, _settings.InfluenceKind);
            influences.Add(new WeightedAngle(weight, angle));
        }

        if (influences.Count == 0)
        {
            return baseAngle;
        }

        influences.Sort(static (left, right) => right.Weight.CompareTo(left.Weight));
        double xVector = Math.Cos(baseAngle);
        double yVector = Math.Sin(baseAngle);
        int count = Math.Min(influences.Count, _settings.MaximumInfluencesPerSample);
        for (int index = 0; index < count; index++)
        {
            WeightedAngle influence = influences[index];
            xVector += Math.Cos(influence.Angle) * influence.Weight;
            yVector += Math.Sin(influence.Angle) * influence.Weight;
        }

        if (Math.Abs(xVector) < 1e-12d && Math.Abs(yVector) < 1e-12d)
        {
            return baseAngle;
        }

        return AngleMath.NormalizeRadians(Math.Atan2(yVector, xVector));
    }

    private static double CalculateInfluenceAngle(
        GeometricPrimitive primitive,
        double deltaX,
        double deltaY,
        PrimitiveFlowInfluenceKind kind)
    {
        return kind switch
        {
            PrimitiveFlowInfluenceKind.AxisAlignment => CalculateAxisAngle(primitive),
            PrimitiveFlowInfluenceKind.BoundaryTangent => CalculateBoundaryTangent(primitive, deltaX, deltaY),
            PrimitiveFlowInfluenceKind.Vortex => AngleMath.NormalizeRadians(Math.Atan2(deltaY, deltaX) + (Math.PI * 0.5d)),
            PrimitiveFlowInfluenceKind.Mixed => CalculateMixedAngle(primitive, deltaX, deltaY),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown primitive flow influence kind.")
        };
    }

    private static double CalculateAxisAngle(GeometricPrimitive primitive)
    {
        double angle = primitive.RotationRadians;
        if (primitive.Height > primitive.Width)
        {
            angle += Math.PI * 0.5d;
        }

        return AngleMath.NormalizeRadians(angle);
    }

    private static double CalculateBoundaryTangent(
        GeometricPrimitive primitive,
        double deltaX,
        double deltaY)
    {
        double cosine = Math.Cos(primitive.RotationRadians);
        double sine = Math.Sin(primitive.RotationRadians);
        double localX = (cosine * deltaX) + (sine * deltaY);
        double localY = (-sine * deltaX) + (cosine * deltaY);
        double radiusX = Math.Max(primitive.Width * 0.5d, 1e-9d);
        double radiusY = Math.Max(primitive.Height * 0.5d, 1e-9d);
        double gradientX = localX / (radiusX * radiusX);
        double gradientY = localY / (radiusY * radiusY);
        if (Math.Abs(gradientX) < 1e-12d && Math.Abs(gradientY) < 1e-12d)
        {
            return CalculateAxisAngle(primitive);
        }

        double localTangent = Math.Atan2(gradientX, -gradientY);
        return AngleMath.NormalizeRadians(localTangent + primitive.RotationRadians);
    }

    private static double CalculateMixedAngle(
        GeometricPrimitive primitive,
        double deltaX,
        double deltaY)
    {
        double axis = CalculateAxisAngle(primitive);
        double boundary = CalculateBoundaryTangent(primitive, deltaX, deltaY);
        double vortex = AngleMath.NormalizeRadians(Math.Atan2(deltaY, deltaX) + (Math.PI * 0.5d));
        double xVector = (Math.Cos(axis) * 0.35d)
            + (Math.Cos(boundary) * 0.45d)
            + (Math.Cos(vortex) * 0.20d);
        double yVector = (Math.Sin(axis) * 0.35d)
            + (Math.Sin(boundary) * 0.45d)
            + (Math.Sin(vortex) * 0.20d);
        return AngleMath.NormalizeRadians(Math.Atan2(yVector, xVector));
    }

    private static void ValidateCoordinate(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Flow-field coordinates must be finite.");
        }
    }

    private readonly record struct WeightedAngle(double Weight, double Angle);
}
