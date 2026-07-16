using FlowPainter.Application.Workloads;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Primitives;
using FlowPainter.Domain.Randomness;

namespace FlowPainter.Application.PrimitiveGeneration;

public sealed class PrimitivePlanOptimizer
{
    public const string PlannerVersion = "primitive-hill-climb-v1";
    private readonly IPrimitiveCandidateFactory _candidateFactory;
    private readonly IPrimitiveMutator _mutator;
    private readonly IPrimitiveScorer _scorer;

    public PrimitivePlanOptimizer()
        : this(
            new DefaultPrimitiveCandidateFactory(),
            new DefaultPrimitiveMutator(),
            new DefaultPrimitiveScorer(new PrimitiveMaskRasterizer()))
    {
    }

    public PrimitivePlanOptimizer(
        IPrimitiveCandidateFactory candidateFactory,
        IPrimitiveMutator mutator,
        IPrimitiveScorer scorer)
    {
        ArgumentNullException.ThrowIfNull(candidateFactory);
        ArgumentNullException.ThrowIfNull(mutator);
        ArgumentNullException.ThrowIfNull(scorer);
        _candidateFactory = candidateFactory;
        _mutator = mutator;
        _scorer = scorer;
    }

    public PrimitivePlan CreatePlan(
        IRgbaPixelSource source,
        DetailMap? detailMap,
        ulong seed,
        PrimitiveGenerationSettings settings,
        IProgress<PrimitiveGenerationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(settings);
        if (detailMap is not null && detailMap.Size != source.Size)
        {
            throw new ArgumentException(
                "The detail map must have the same dimensions as the source image.",
                nameof(detailMap));
        }

        WorkloadBudgetPolicy.EnsureGenerationWithinBudget(
            GenerationWorkEstimator.EstimatePrimitives(source.Size, settings));
        cancellationToken.ThrowIfCancellationRequested();
        Rgba32[] sourcePixels = CopyPixels(source);
        Rgba32 background = CalculateAverageColor(sourcePixels);
        Rgba32[] currentPixels = new Rgba32[sourcePixels.Length];
        Array.Fill(currentPixels, background);
        double currentError = CalculateTotalError(source.Size, sourcePixels, currentPixels, detailMap, settings.DetailErrorWeight);
        DeterministicRandom random = new(seed);
        List<GeometricPrimitive> primitives = new(settings.PrimitiveCount);

        progress?.Report(new PrimitiveGenerationProgress(
            PrimitiveGenerationStage.Preparing,
            0,
            settings.PrimitiveCount,
            0d,
            currentError));

        for (int primitiveIndex = 0; primitiveIndex < settings.PrimitiveCount; primitiveIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PrimitiveScore? best = null;
            for (int candidateIndex = 0; candidateIndex < settings.CandidatesPerStep; candidateIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                GeometricPrimitive candidate = _candidateFactory.Create(
                    primitiveIndex,
                    detailMap,
                    settings,
                    random);
                PrimitiveScore score = _scorer.Score(
                    source.Size,
                    sourcePixels,
                    currentPixels,
                    detailMap,
                    candidate,
                    settings);
                if (best is null || score.Improvement > best.Improvement)
                {
                    best = score;
                }
            }

            if (best is null)
            {
                break;
            }

            int mutationBudget = GetMutationBudget(best.Primitive, detailMap, settings);
            for (int mutationIndex = 0; mutationIndex < mutationBudget; mutationIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                GeometricPrimitive mutated = _mutator.Mutate(best.Primitive, settings, random);
                PrimitiveScore score = _scorer.Score(
                    source.Size,
                    sourcePixels,
                    currentPixels,
                    detailMap,
                    mutated,
                    settings);
                if (score.Improvement > best.Improvement)
                {
                    best = score;
                }
            }

            if (best.Improvement <= 0d)
            {
                break;
            }

            Apply(currentPixels, best);
            currentError = Math.Max(0d, currentError - best.Improvement);
            primitives.Add(best.Primitive);
            progress?.Report(new PrimitiveGenerationProgress(
                PrimitiveGenerationStage.Searching,
                primitives.Count,
                settings.PrimitiveCount,
                (double)primitives.Count / settings.PrimitiveCount,
                currentError));
        }

        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new PrimitiveGenerationProgress(
            PrimitiveGenerationStage.Completed,
            primitives.Count,
            settings.PrimitiveCount,
            1d,
            currentError));
        return new PrimitivePlan(source.Size, seed, background, primitives, PlannerVersion);
    }

    private static int GetMutationBudget(
        GeometricPrimitive primitive,
        DetailMap? detailMap,
        PrimitiveGenerationSettings settings)
    {
        if (detailMap is null || settings.MutationIterations == 0 || settings.DetailSearchInfluence == 0d)
        {
            return settings.MutationIterations;
        }

        double localDetail = detailMap.SampleNearest(primitive.Center);
        double scaled = settings.MutationIterations
            * (1d + (localDetail * settings.DetailSearchInfluence));
        return Math.Min(
            PrimitiveGenerationSettings.MaximumMutationIterations,
            checked((int)Math.Round(scaled, MidpointRounding.AwayFromZero)));
    }

    private static Rgba32[] CopyPixels(IRgbaPixelSource source)
    {
        if (source is RgbaImage image)
        {
            return image.CopyPixels();
        }

        Rgba32[] pixels = new Rgba32[checked((int)source.Size.PixelCount)];
        for (int y = 0; y < source.Size.Height; y++)
        {
            for (int x = 0; x < source.Size.Width; x++)
            {
                NormalizedPoint point = new(
                    (x + 0.5d) / source.Size.Width,
                    (y + 0.5d) / source.Size.Height);
                pixels[checked((y * source.Size.Width) + x)] = source.SampleNearest(point);
            }
        }

        return pixels;
    }

    private static Rgba32 CalculateAverageColor(ReadOnlySpan<Rgba32> pixels)
    {
        long red = 0L;
        long green = 0L;
        long blue = 0L;
        foreach (Rgba32 pixel in pixels)
        {
            red += pixel.Red;
            green += pixel.Green;
            blue += pixel.Blue;
        }

        return Rgba32.Opaque(
            checked((byte)Math.Round((double)red / pixels.Length, MidpointRounding.AwayFromZero)),
            checked((byte)Math.Round((double)green / pixels.Length, MidpointRounding.AwayFromZero)),
            checked((byte)Math.Round((double)blue / pixels.Length, MidpointRounding.AwayFromZero)));
    }

    private static double CalculateTotalError(
        ImageSize size,
        ReadOnlySpan<Rgba32> source,
        ReadOnlySpan<Rgba32> current,
        DetailMap? detailMap,
        double detailErrorWeight)
    {
        double error = 0d;
        for (int index = 0; index < source.Length; index++)
        {
            Rgba32 target = source[index];
            Rgba32 existing = current[index];
            double red = existing.Red - target.Red;
            double green = existing.Green - target.Green;
            double blue = existing.Blue - target.Blue;
            double weight = 1d;
            if (detailMap is not null)
            {
                int x = index % size.Width;
                int y = index / size.Width;
                weight += detailErrorWeight * detailMap[x, y];
            }

            error += weight * ((red * red) + (green * green) + (blue * blue));
        }

        return error;
    }

    private static void Apply(Rgba32[] currentPixels, PrimitiveScore score)
    {
        foreach (int index in score.Mask.PixelIndices)
        {
            currentPixels[index] = DefaultPrimitiveScorer.Blend(
                currentPixels[index],
                score.Primitive.Color);
        }
    }
}
