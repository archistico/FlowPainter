using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.Hybrid;
using FlowPainter.Application.PrimitiveGeneration;
using FlowPainter.Domain.Generation;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Workloads;

public static class GenerationWorkEstimator
{
    public static GenerationWorkEstimate Estimate(
        GenerativeMode mode,
        ImageSize workingSize,
        FlowPainterSettings flowSettings,
        PrimitiveGenerationSettings primitiveSettings,
        HybridGenerationSettings hybridSettings)
    {
        ArgumentNullException.ThrowIfNull(flowSettings);
        ArgumentNullException.ThrowIfNull(primitiveSettings);
        ArgumentNullException.ThrowIfNull(hybridSettings);
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown generative mode.");
        }

        return mode switch
        {
            GenerativeMode.FlowPainting => EstimateFlow(flowSettings),
            GenerativeMode.GeometricPrimitives => EstimatePrimitives(workingSize, primitiveSettings),
            GenerativeMode.Hybrid => EstimateHybrid(workingSize, flowSettings, primitiveSettings, hybridSettings),
            _ => throw new InvalidOperationException("Unknown generative mode.")
        };
    }

    public static GenerationWorkEstimate EstimateFlow(FlowPainterSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        int maximumSegmentCount = EstimateMaximumFlowSegmentCount(settings);
        long segmentSteps = checked((long)settings.StrokeCount * maximumSegmentCount);
        return new GenerationWorkEstimate(
            GenerativeMode.FlowPainting,
            segmentSteps,
            0L,
            0L);
    }

    public static GenerationWorkEstimate EstimatePrimitives(
        ImageSize workingSize,
        PrimitiveGenerationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        PrimitiveWork primitiveWork = CalculatePrimitiveWork(workingSize, settings, settings.PrimitiveCount);
        return new GenerationWorkEstimate(
            GenerativeMode.GeometricPrimitives,
            0L,
            primitiveWork.ScoreAttempts,
            primitiveWork.PixelEvaluations);
    }

    public static GenerationWorkEstimate EstimateHybrid(
        ImageSize workingSize,
        FlowPainterSettings flowSettings,
        PrimitiveGenerationSettings primitiveSettings,
        HybridGenerationSettings hybridSettings)
    {
        ArgumentNullException.ThrowIfNull(flowSettings);
        ArgumentNullException.ThrowIfNull(primitiveSettings);
        ArgumentNullException.ThrowIfNull(hybridSettings);

        int primitiveCount = ScaleCount(
            primitiveSettings.PrimitiveCount,
            hybridSettings.PrimitiveBudgetFraction);
        int flowStrokeCount = ScaleCount(
            flowSettings.StrokeCount,
            hybridSettings.FlowBudgetFraction);
        int refinementStrokeCount = ScaleCount(
            flowSettings.StrokeCount,
            hybridSettings.RefinementBudgetFraction);
        int maximumSegmentCount = EstimateMaximumFlowSegmentCount(flowSettings);
        long flowSegmentSteps = checked(
            (long)(flowStrokeCount + refinementStrokeCount)
            * maximumSegmentCount);
        PrimitiveWork primitiveWork = CalculatePrimitiveWork(
            workingSize,
            primitiveSettings,
            primitiveCount);

        return new GenerationWorkEstimate(
            GenerativeMode.Hybrid,
            flowSegmentSteps,
            primitiveWork.ScoreAttempts,
            primitiveWork.PixelEvaluations);
    }


    private static int EstimateMaximumFlowSegmentCount(FlowPainterSettings settings)
    {
        double detailMultiplier = Math.Max(
            settings.DetailInfluence.DetailedSegmentMultiplier,
            settings.DetailInfluence.BackgroundSegmentMultiplier);
        return Math.Clamp(
            checked((int)Math.Ceiling(settings.SegmentCount * detailMultiplier)),
            2,
            FlowPainterSettings.MaximumSegmentCount);
    }

    private static PrimitiveWork CalculatePrimitiveWork(
        ImageSize workingSize,
        PrimitiveGenerationSettings settings,
        int primitiveCount)
    {
        int mutationIterations = EstimateMaximumMutationIterations(settings);
        long attemptsPerPrimitive = checked((long)settings.CandidatesPerStep + mutationIterations);
        long scoreAttempts = checked(primitiveCount * attemptsPerPrimitive);
        long pixelsPerAttempt = EstimateMaximumRasterPixels(
            workingSize,
            settings.MaximumSize);
        long pixelEvaluations = checked(scoreAttempts * pixelsPerAttempt);
        return new PrimitiveWork(scoreAttempts, pixelEvaluations);
    }

    private static long EstimateMaximumRasterPixels(
        ImageSize workingSize,
        double maximumSize)
    {
        double widthPixels = workingSize.Width * maximumSize;
        double heightPixels = workingSize.Height * maximumSize;
        double diagonalPixels = Math.Sqrt(
            (widthPixels * widthPixels)
            + (heightPixels * heightPixels));
        long maximumSpan = Math.Max(
            1L,
            checked((long)Math.Ceiling(diagonalPixels) + 2L));
        long horizontalSpan = Math.Min(workingSize.Width, maximumSpan);
        long verticalSpan = Math.Min(workingSize.Height, maximumSpan);
        return checked(horizontalSpan * verticalSpan);
    }

    private static int EstimateMaximumMutationIterations(PrimitiveGenerationSettings settings)
    {
        double scaled = settings.MutationIterations
            * (1d + settings.DetailSearchInfluence);
        return Math.Min(
            PrimitiveGenerationSettings.MaximumMutationIterations,
            checked((int)Math.Round(scaled, MidpointRounding.AwayFromZero)));
    }

    private static int ScaleCount(int total, double fraction)
    {
        return Math.Max(
            1,
            checked((int)Math.Round(total * fraction, MidpointRounding.AwayFromZero)));
    }

    private readonly record struct PrimitiveWork(
        long ScoreAttempts,
        long PixelEvaluations);
}
