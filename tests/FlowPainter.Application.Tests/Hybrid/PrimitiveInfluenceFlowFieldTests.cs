using FlowPainter.Application.FlowPainting.Fields;
using FlowPainter.Application.Hybrid;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Hybrid;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Primitives;

namespace FlowPainter.Application.Tests.Hybrid;

public sealed class PrimitiveInfluenceFlowFieldTests
{
    [Fact]
    public void ZeroStrengthPreservesBaseField()
    {
        PrimitiveInfluenceFlowField field = CreateField(
            PrimitiveFlowInfluenceKind.Mixed,
            influenceStrength: 0d,
            baseAngle: 1.25d);

        Assert.Equal(1.25d, field.SampleAngle(0.55d, 0.5d), 12);
    }

    [Fact]
    public void AxisAlignmentPullsFieldTowardPrimitiveMajorAxis()
    {
        PrimitiveInfluenceFlowField field = CreateField(
            PrimitiveFlowInfluenceKind.AxisAlignment,
            influenceStrength: 1d,
            baseAngle: Math.PI * 0.5d);

        double angle = field.SampleAngle(0.5d, 0.5d);

        Assert.InRange(angle, 0.70d, 0.88d);
    }

    [Fact]
    public void BoundaryTangentFollowsEllipseAtRightEdge()
    {
        PrimitiveInfluenceFlowField field = CreateField(
            PrimitiveFlowInfluenceKind.BoundaryTangent,
            influenceStrength: 1d,
            baseAngle: 0d);

        double angle = field.SampleAngle(0.65d, 0.5d);

        Assert.InRange(angle, 0.45d, 1.2d);
    }

    [Fact]
    public void VortexTurnsAroundPrimitiveCenter()
    {
        PrimitiveInfluenceFlowField field = CreateField(
            PrimitiveFlowInfluenceKind.Vortex,
            influenceStrength: 1d,
            baseAngle: 0d);

        double angle = field.SampleAngle(0.6d, 0.5d);

        Assert.InRange(angle, 0.45d, 1.2d);
    }

    [Fact]
    public void SamplesOutsideInfluenceRadiusPreserveBaseField()
    {
        PrimitiveInfluenceFlowField field = CreateField(
            PrimitiveFlowInfluenceKind.Mixed,
            influenceStrength: 1d,
            baseAngle: 0.35d,
            radiusMultiplier: 0.5d);

        Assert.Equal(0.35d, field.SampleAngle(0.99d, 0.99d), 12);
    }

    [Fact]
    public void RepeatedSamplesAreDeterministic()
    {
        PrimitiveInfluenceFlowField field = CreateField(
            PrimitiveFlowInfluenceKind.Mixed,
            influenceStrength: 0.8d,
            baseAngle: 0.4d);

        double first = field.SampleAngle(0.56d, 0.47d);
        double second = field.SampleAngle(0.56d, 0.47d);

        Assert.Equal(first, second, 12);
    }

    private static PrimitiveInfluenceFlowField CreateField(
        PrimitiveFlowInfluenceKind kind,
        double influenceStrength,
        double baseAngle,
        double radiusMultiplier = 2d)
    {
        GeometricPrimitive primitive = new(
            0,
            PrimitiveKind.Ellipse,
            new NormalizedPoint(0.5d, 0.5d),
            0.4d,
            0.2d,
            0d,
            Rgba32.Opaque(100, 100, 100));
        PrimitivePlan plan = new(
            new ImageSize(20, 20),
            1UL,
            Rgba32.Opaque(20, 20, 20),
            [primitive],
            "test");
        HybridGenerationSettings settings = new(
            influenceKind: kind,
            influenceStrength: influenceStrength,
            influenceRadiusMultiplier: radiusMultiplier);
        return new PrimitiveInfluenceFlowField(new ConstantFlowField(baseAngle), plan, settings);
    }

    private sealed class ConstantFlowField : IFlowField
    {
        private readonly double _angle;

        public ConstantFlowField(double angle)
        {
            _angle = angle;
        }

        public double SampleAngle(double x, double y)
        {
            return _angle;
        }
    }
}
