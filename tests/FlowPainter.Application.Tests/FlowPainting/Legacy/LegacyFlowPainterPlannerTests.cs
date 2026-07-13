using FlowPainter.Application.FlowPainting.Legacy;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Strokes;

namespace FlowPainter.Application.Tests.FlowPainting.Legacy;

public sealed class LegacyFlowPainterPlannerTests
{
    private const ulong CharacterizationSeed = 0x0123456789ABCDEFUL;

    [Fact]
    public void RepositoryFixtureLoadsWithoutNetworkAccess()
    {
        LegacyFlowFixture fixture = LegacyFlowFixture.Load();

        Assert.Equal("legacy-flow-fixture-v1", fixture.Version);
        Assert.Equal(new ImageSize(4, 3), fixture.Source.Size);
        Assert.Equal(Rgba32.Opaque(255, 255, 255), fixture.Source[3, 0]);
        Assert.Equal(16d, fixture.DensityMap[1, 2]);
    }

    [Fact]
    public void CreatePlanProducesStableCharacterizationPlan()
    {
        LegacyFlowFixture fixture = LegacyFlowFixture.Load();
        RecordingFieldFactory factory = new(new ConstantField(0d));
        LegacyFlowPainterPlanner planner = new(factory);
        LegacyFlowPainterSettings settings = new(strokeCount: 3, segmentCount: 2);

        StrokePlan plan = planner.CreatePlan(
            fixture.Source,
            fixture.DensityMap,
            CharacterizationSeed,
            settings);

        Assert.Equal(CharacterizationSeed, plan.Seed);
        Assert.Equal(1_334_057_644, plan.FieldSeed);
        Assert.Equal(plan.FieldSeed, factory.ReceivedSeed);
        Assert.Equal(512, plan.ReferenceMaximumDimension);
        Assert.Equal(StrokePlanBackgroundMode.SourceImage, plan.BackgroundMode);
        Assert.Equal(StrokePlan.LegacyPlannerVersion, plan.PlannerVersion);
        Assert.Collection(
            plan.Strokes,
            _ => { },
            _ => { },
            _ => { });
        AssertStroke(
            plan.Strokes[0],
            Rgba32.Opaque(255, 255, 255),
            0.009813353483606024d,
            [
                (0.8337909344596774d, 0.18580193412474622d),
                (0.8537909344596775d, 0.18580193412474622d),
                (0.8737909344596775d, 0.18580193412474622d)
            ]);
        AssertStroke(
            plan.Strokes[1],
            Rgba32.Opaque(128, 128, 128),
            0.019284018378454275d,
            [
                (0.08099885568132692d, 0.7226006426794261d),
                (0.10099885568132692d, 0.7226006426794261d),
                (0.12099885568132693d, 0.7226006426794261d)
            ]);
        AssertStroke(
            plan.Strokes[2],
            Rgba32.Opaque(128, 128, 128),
            0.01521028909918313d,
            [
                (0.1504030464670475d, 0.8036008955917952d),
                (0.1704030464670475d, 0.8036008955917952d),
                (0.1904030464670475d, 0.8036008955917952d)
            ]);
    }

    [Fact]
    public void CreatePlanIsRepeatableForEqualInputs()
    {
        LegacyFlowFixture fixture = LegacyFlowFixture.Load();
        LegacyFlowPainterSettings settings = new(strokeCount: 10, segmentCount: 4);
        LegacyFlowPainterPlanner firstPlanner = new(new RecordingFieldFactory(new ConstantField(0.125d)));
        LegacyFlowPainterPlanner secondPlanner = new(new RecordingFieldFactory(new ConstantField(0.125d)));

        StrokePlan first = firstPlanner.CreatePlan(fixture.Source, fixture.DensityMap, 42UL, settings);
        StrokePlan second = secondPlanner.CreatePlan(fixture.Source, fixture.DensityMap, 42UL, settings);

        AssertPlansEqual(first, second);
    }

    [Fact]
    public void CreatePlanUsesCircularDistanceAcrossTauBoundary()
    {
        RgbaImage image = CreateSinglePixelImage();
        LegacyDensityMap density = new(image.Size, [1d]);
        LegacyFlowPainterPlanner planner = new(
            new RecordingFieldFactory(new SequencedField([0.99d, 0.01d])));
        LegacyFlowPainterSettings settings = new(strokeCount: 1, segmentCount: 2);

        StrokePlan plan = planner.CreatePlan(image, density, 1UL, settings);

        Assert.Collection(
            plan.Strokes[0].Points,
            _ => { },
            _ => { },
            _ => { });
    }

    [Fact]
    public void CreatePlanStopsPathWhenCurveExceedsLimit()
    {
        RgbaImage image = CreateSinglePixelImage();
        LegacyDensityMap density = new(image.Size, [1d]);
        LegacyFlowPainterPlanner planner = new(
            new RecordingFieldFactory(new SequencedField([0d, 0.25d])));
        LegacyFlowPainterSettings settings = new(strokeCount: 1, segmentCount: 2);

        StrokePlan plan = planner.CreatePlan(image, density, 1UL, settings);

        Assert.Collection(
            plan.Strokes[0].Points,
            _ => { },
            _ => { });
    }


    [Fact]
    public void CreatePlanRetainsLegacyPostPathFieldSample()
    {
        RgbaImage image = CreateSinglePixelImage();
        LegacyDensityMap density = new(image.Size, [1d]);
        CountingField field = new(0d);
        LegacyFlowPainterPlanner planner = new(new RecordingFieldFactory(field));
        LegacyFlowPainterSettings settings = new(strokeCount: 1, segmentCount: 2);

        _ = planner.CreatePlan(image, density, 1UL, settings);

        Assert.Equal(3, field.SampleCount);
    }

    [Fact]
    public void CreatePlanPreservesLegacyOutOfBoundsPathBehaviour()
    {
        RgbaImage image = CreateSinglePixelImage();
        LegacyDensityMap density = new(image.Size, [1_000d]);
        LegacyFlowPainterPlanner planner = new(new RecordingFieldFactory(new ConstantField(0d)));
        LegacyFlowPainterSettings settings = new(
            strokeCount: 1,
            segmentCount: 1,
            lengthScale: 0.1d);

        StrokePlan plan = planner.CreatePlan(image, density, 1UL, settings);

        Assert.False(plan.Strokes[0].Points[^1].IsInsideCanvas);
    }

    [Fact]
    public void CreatePlanRejectsMismatchedSourceAndDensityDimensions()
    {
        RgbaImage image = CreateSinglePixelImage();
        LegacyDensityMap density = new(new ImageSize(2, 1), [1d, 1d]);
        LegacyFlowPainterPlanner planner = new(new RecordingFieldFactory(new ConstantField(0d)));

        Assert.Throws<ArgumentException>(() => planner.CreatePlan(image, density, 1UL));
    }

    [Fact]
    public void CreatePlanRejectsNonFiniteFieldValue()
    {
        RgbaImage image = CreateSinglePixelImage();
        LegacyDensityMap density = new(image.Size, [1d]);
        LegacyFlowPainterPlanner planner = new(new RecordingFieldFactory(new ConstantField(double.NaN)));
        LegacyFlowPainterSettings settings = new(strokeCount: 1, segmentCount: 1);

        Assert.Throws<InvalidOperationException>(
            () => planner.CreatePlan(image, density, 1UL, settings));
    }

    private static RgbaImage CreateSinglePixelImage()
    {
        return new RgbaImage(new ImageSize(1, 1), [Rgba32.Opaque(10, 20, 30)]);
    }

    private static void AssertStroke(
        FlowStroke stroke,
        Rgba32 expectedColor,
        double expectedWidth,
        IReadOnlyList<(double X, double Y)> expectedPoints)
    {
        Assert.Equal(expectedColor, stroke.Color);
        AssertClose(expectedWidth, stroke.WidthRelativeToReference);
        Assert.Empty(expectedPoints.Skip(stroke.Points.Count));
        Assert.Empty(stroke.Points.Skip(expectedPoints.Count));

        for (int index = 0; index < expectedPoints.Count; index++)
        {
            AssertClose(expectedPoints[index].X, stroke.Points[index].X);
            AssertClose(expectedPoints[index].Y, stroke.Points[index].Y);
        }
    }

    private static void AssertPlansEqual(StrokePlan expected, StrokePlan actual)
    {
        Assert.Equal(expected.SourceSize, actual.SourceSize);
        Assert.Equal(expected.Seed, actual.Seed);
        Assert.Equal(expected.FieldSeed, actual.FieldSeed);
        Assert.Equal(expected.ReferenceMaximumDimension, actual.ReferenceMaximumDimension);
        Assert.Equal(expected.BackgroundMode, actual.BackgroundMode);
        Assert.Equal(expected.PlannerVersion, actual.PlannerVersion);
        Assert.Empty(expected.Strokes.Skip(actual.Strokes.Count));
        Assert.Empty(actual.Strokes.Skip(expected.Strokes.Count));

        for (int strokeIndex = 0; strokeIndex < expected.Strokes.Count; strokeIndex++)
        {
            FlowStroke expectedStroke = expected.Strokes[strokeIndex];
            FlowStroke actualStroke = actual.Strokes[strokeIndex];
            Assert.Equal(expectedStroke.Index, actualStroke.Index);
            Assert.Equal(expectedStroke.Color, actualStroke.Color);
            AssertClose(expectedStroke.WidthRelativeToReference, actualStroke.WidthRelativeToReference);
            Assert.Empty(expectedStroke.Points.Skip(actualStroke.Points.Count));
            Assert.Empty(actualStroke.Points.Skip(expectedStroke.Points.Count));

            for (int pointIndex = 0; pointIndex < expectedStroke.Points.Count; pointIndex++)
            {
                Assert.Equal(expectedStroke.Points[pointIndex], actualStroke.Points[pointIndex]);
            }
        }
    }

    private static void AssertClose(double expected, double actual)
    {
        Assert.InRange(Math.Abs(expected - actual), 0d, 1e-12d);
    }

    private sealed class RecordingFieldFactory : ILegacyScalarFieldFactory
    {
        private readonly ILegacyScalarField _field;

        public RecordingFieldFactory(ILegacyScalarField field)
        {
            _field = field;
        }

        public int ReceivedSeed { get; private set; } = -1;

        public ILegacyScalarField Create(int seed)
        {
            ReceivedSeed = seed;
            return _field;
        }
    }

    private sealed class ConstantField : ILegacyScalarField
    {
        private readonly double _value;

        public ConstantField(double value)
        {
            _value = value;
        }

        public double Sample(double x, double y)
        {
            _ = x;
            _ = y;
            return _value;
        }
    }


    private sealed class CountingField : ILegacyScalarField
    {
        private readonly double _value;

        public CountingField(double value)
        {
            _value = value;
        }

        public int SampleCount { get; private set; }

        public double Sample(double x, double y)
        {
            _ = x;
            _ = y;
            SampleCount++;
            return _value;
        }
    }

    private sealed class SequencedField : ILegacyScalarField
    {
        private readonly IReadOnlyList<double> _values;
        private int _index;

        public SequencedField(IReadOnlyList<double> values)
        {
            _values = values;
        }

        public double Sample(double x, double y)
        {
            _ = x;
            _ = y;
            int index = Math.Min(_index, _values.Count - 1);
            _index++;
            return _values[index];
        }
    }
}
