using FlowPainter.Application.PrimitiveGeneration;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Primitives;

namespace FlowPainter.Application.Tests.PrimitiveGeneration;

public sealed class PrimitivePlanOptimizerTests
{
    [Fact]
    public void CreatePlanIsDeterministicForEqualInputs()
    {
        RgbaImage source = CreateSplitImage();
        PrimitiveGenerationSettings settings = CreateFastSettings();
        PrimitivePlanOptimizer optimizer = new();

        PrimitivePlan first = optimizer.CreatePlan(source, null, 123UL, settings);
        PrimitivePlan second = optimizer.CreatePlan(source, null, 123UL, settings);

        Assert.Equal(first.BackgroundColor, second.BackgroundColor);
        Assert.Equal(first.Primitives.Count, second.Primitives.Count);
        Assert.Equal(first.Primitives.Select(ToSignature), second.Primitives.Select(ToSignature));
    }

    [Fact]
    public void CreatePlanProducesImprovingPrimitivesForNonUniformImage()
    {
        RgbaImage source = CreateSplitImage();
        PrimitivePlanOptimizer optimizer = new();

        PrimitivePlan plan = optimizer.CreatePlan(source, null, 9UL, CreateFastSettings());

        Assert.NotEmpty(plan.Primitives);
        Assert.All(plan.Primitives, primitive => Assert.True(primitive.Color.Alpha > 0));
        Assert.Equal(PrimitivePlanOptimizer.PlannerVersion, plan.PlannerVersion);
    }

    [Fact]
    public void CreatePlanUsesContiguousIndexesWhenSearchStopsEarly()
    {
        RgbaImage source = CreateSplitImage();
        PrimitivePlan plan = new PrimitivePlanOptimizer().CreatePlan(
            source,
            null,
            44UL,
            CreateFastSettings());

        Assert.Equal(Enumerable.Range(0, plan.Primitives.Count), plan.Primitives.Select(value => value.Index));
    }

    [Fact]
    public void CreatePlanHonorsPreCancelledToken()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        PrimitivePlanOptimizer optimizer = new();

        Assert.ThrowsAny<OperationCanceledException>(() => optimizer.CreatePlan(
            CreateSplitImage(),
            null,
            1UL,
            CreateFastSettings(),
            cancellationToken: cancellation.Token));
    }

    [Fact]
    public void CreatePlanReportsCompletion()
    {
        RecordingProgress<PrimitiveGenerationProgress> progress = new();
        PrimitivePlanOptimizer optimizer = new();

        PrimitivePlan plan = optimizer.CreatePlan(
            CreateSplitImage(),
            DetailMap.CreateUniform(new ImageSize(16, 12), 0.5f),
            5UL,
            CreateFastSettings(),
            progress);

        Assert.NotNull(plan);
        Assert.Equal(PrimitiveGenerationStage.Preparing, progress.Values[0].Stage);
        Assert.Equal(PrimitiveGenerationStage.Completed, progress.Values[^1].Stage);
        Assert.Equal(1d, progress.Values[^1].Fraction);
    }

    [Fact]
    public void CreatePlanAllocatesMoreMutationsToDetailedArea()
    {
        CountingCandidateFactory factory = new();
        CountingMutator mutator = new();
        PositiveScorer scorer = new();
        PrimitivePlanOptimizer optimizer = new(factory, mutator, scorer);
        ImageSize size = new(2, 2);
        RgbaImage source = new(size, Enumerable.Repeat(Rgba32.Opaque(10, 10, 10), 4).ToArray());
        PrimitiveGenerationSettings settings = new(
            primitiveCount: 1,
            candidatesPerStep: 1,
            mutationIterations: 2,
            detailSearchInfluence: 2d,
            allowedKinds: PrimitiveKindSet.Rectangle);

        PrimitivePlan plan = optimizer.CreatePlan(
            source,
            DetailMap.CreateUniform(size, 1f),
            1UL,
            settings);

        Assert.Single(plan.Primitives);
        Assert.Equal(1, factory.Calls);
        Assert.Equal(6, mutator.Calls);
        Assert.Equal(7, scorer.Calls);
    }

    [Fact]
    public void CreatePlanRejectsMismatchedDetailMap()
    {
        PrimitivePlanOptimizer optimizer = new();

        Assert.Throws<ArgumentException>(() => optimizer.CreatePlan(
            CreateSplitImage(),
            DetailMap.CreateUniform(new ImageSize(4, 4), 0.5f),
            1UL,
            CreateFastSettings()));
    }

    private static PrimitiveGenerationSettings CreateFastSettings()
    {
        return new PrimitiveGenerationSettings(
            primitiveCount: 12,
            candidatesPerStep: 6,
            mutationIterations: 4,
            minimumSize: 0.08d,
            maximumSize: 0.6d,
            opacity: 0.8d,
            allowedKinds: PrimitiveKindSet.Rectangle | PrimitiveKindSet.Ellipse);
    }

    private static RgbaImage CreateSplitImage()
    {
        ImageSize size = new(16, 12);
        Rgba32[] pixels = new Rgba32[checked((int)size.PixelCount)];
        for (int y = 0; y < size.Height; y++)
        {
            for (int x = 0; x < size.Width; x++)
            {
                pixels[checked((y * size.Width) + x)] = x < size.Width / 2
                    ? Rgba32.Opaque(220, 40, 30)
                    : Rgba32.Opaque(30, 70, 220);
            }
        }

        return new RgbaImage(size, pixels);
    }

    private static string ToSignature(GeometricPrimitive primitive)
    {
        return FormattableString.Invariant(
            $"{primitive.Index}|{primitive.Kind}|{primitive.Center.X:F12}|{primitive.Center.Y:F12}|{primitive.Width:F12}|{primitive.Height:F12}|{primitive.RotationRadians:F12}|{primitive.Color.ToRgbaUInt32()}");
    }

    private sealed class CountingCandidateFactory : IPrimitiveCandidateFactory
    {
        public int Calls { get; private set; }

        public GeometricPrimitive Create(
            int index,
            DetailMap? detailMap,
            PrimitiveGenerationSettings settings,
            FlowPainter.Domain.Randomness.IRandomSource random)
        {
            Calls++;
            return new GeometricPrimitive(
                index,
                PrimitiveKind.Rectangle,
                new FlowPainter.Domain.Geometry.NormalizedPoint(0.5d, 0.5d),
                1d,
                1d,
                0d,
                Rgba32.Opaque(0, 0, 0));
        }
    }

    private sealed class CountingMutator : IPrimitiveMutator
    {
        public int Calls { get; private set; }

        public GeometricPrimitive Mutate(
            GeometricPrimitive primitive,
            PrimitiveGenerationSettings settings,
            FlowPainter.Domain.Randomness.IRandomSource random)
        {
            Calls++;
            return primitive;
        }
    }

    private sealed class PositiveScorer : IPrimitiveScorer
    {
        public int Calls { get; private set; }

        public PrimitiveScore Score(
            ImageSize size,
            ReadOnlyMemory<Rgba32> sourcePixels,
            ReadOnlyMemory<Rgba32> currentPixels,
            DetailMap? detailMap,
            GeometricPrimitive candidate,
            PrimitiveGenerationSettings settings)
        {
            Calls++;
            return new PrimitiveScore(
                candidate.WithColor(Rgba32.Opaque(20, 20, 20)),
                new PrimitiveRasterMask(size, []),
                1d);
        }
    }

}
