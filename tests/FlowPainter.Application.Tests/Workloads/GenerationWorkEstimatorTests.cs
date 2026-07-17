using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.Hybrid;
using FlowPainter.Application.PrimitiveGeneration;
using FlowPainter.Application.Workloads;
using FlowPainter.Domain.Generation;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.Workloads;

public sealed class GenerationWorkEstimatorTests
{
    [Fact]
    public void EstimateFlowIncludesMaximumLocalSegmentMultiplier()
    {
        FlowPainterSettings settings = new(strokeCount: 120, segmentCount: 24);

        GenerationWorkEstimate estimate = GenerationWorkEstimator.EstimateFlow(settings);

        Assert.Equal(GenerativeMode.FlowPainting, estimate.Mode);
        Assert.Equal(4_800L, estimate.FlowSegmentSteps);
        Assert.Equal(0L, estimate.PrimitiveScoreAttempts);
    }


    [Fact]
    public void EstimateFlowCapsLocalSegmentsAtSupportedMaximum()
    {
        FlowPainterSettings settings = new(
            strokeCount: 2,
            segmentCount: FlowPainterSettings.MaximumSegmentCount,
            detailInfluence: new DetailInfluenceSettings(detailedSegmentMultiplier: 4d));

        GenerationWorkEstimate estimate = GenerationWorkEstimator.EstimateFlow(settings);

        Assert.Equal(2L * FlowPainterSettings.MaximumSegmentCount, estimate.FlowSegmentSteps);
    }

    [Fact]
    public void EstimateFlowUsesLargerBackgroundOrDetailedSegmentPolicy()
    {
        FlowPainterSettings settings = new(
            strokeCount: 10,
            segmentCount: 10,
            detailInfluence: new DetailInfluenceSettings(
                detailedSegmentMultiplier: 0.5d,
                backgroundSegmentMultiplier: 1.8d));

        GenerationWorkEstimate estimate = GenerationWorkEstimator.EstimateFlow(settings);

        Assert.Equal(180L, estimate.FlowSegmentSteps);
    }

    [Fact]
    public void EstimatePrimitivesIncludesWorstCaseDetailMutationScaling()
    {
        PrimitiveGenerationSettings settings = new(
            primitiveCount: 10,
            candidatesPerStep: 3,
            mutationIterations: 2,
            minimumSize: 0.1d,
            maximumSize: 0.5d,
            detailSearchInfluence: 1d);

        GenerationWorkEstimate estimate = GenerationWorkEstimator.EstimatePrimitives(
            new ImageSize(100, 50),
            settings);

        Assert.Equal(70L, estimate.PrimitiveScoreAttempts);
        Assert.Equal(203_000L, estimate.PrimitivePixelEvaluations);
    }

    [Fact]
    public void EstimateHybridUsesComposerBudgetFractions()
    {
        FlowPainterSettings flow = new(strokeCount: 100, segmentCount: 10);
        PrimitiveGenerationSettings primitives = new(
            primitiveCount: 100,
            candidatesPerStep: 2,
            mutationIterations: 0,
            minimumSize: 0.1d,
            maximumSize: 0.5d);
        HybridGenerationSettings hybrid = new(
            primitiveBudgetFraction: 0.35d,
            flowBudgetFraction: 0.45d,
            refinementBudgetFraction: 0.20d);

        GenerationWorkEstimate estimate = GenerationWorkEstimator.EstimateHybrid(
            new ImageSize(20, 10),
            flow,
            primitives,
            hybrid);

        Assert.Equal(GenerativeMode.Hybrid, estimate.Mode);
        Assert.Equal(1_105L, estimate.FlowSegmentSteps);
        Assert.Equal(70L, estimate.PrimitiveScoreAttempts);
        Assert.Equal(9_800L, estimate.PrimitivePixelEvaluations);
    }

    [Fact]
    public void PolicyRejectsUnsafeFlowComposition()
    {
        FlowPainterSettings settings = new(
            strokeCount: FlowPainterSettings.MaximumStrokeCount,
            segmentCount: FlowPainterSettings.MaximumSegmentCount);
        GenerationWorkEstimate estimate = GenerationWorkEstimator.EstimateFlow(settings);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => WorkloadBudgetPolicy.EnsureGenerationWithinBudget(estimate));

        Assert.Contains("flow-segment steps", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PolicyRejectsUnsafePrimitiveComposition()
    {
        PrimitiveGenerationSettings settings = new(
            primitiveCount: PrimitiveGenerationSettings.MaximumPrimitiveCount,
            candidatesPerStep: PrimitiveGenerationSettings.MaximumCandidateCount,
            mutationIterations: PrimitiveGenerationSettings.MaximumMutationIterations,
            minimumSize: 0.1d,
            maximumSize: 1d,
            detailSearchInfluence: 4d);
        GenerationWorkEstimate estimate = GenerationWorkEstimator.EstimatePrimitives(
            new ImageSize(1024, 1024),
            settings);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => WorkloadBudgetPolicy.EnsureGenerationWithinBudget(estimate));

        Assert.Contains("primitive score attempts", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PolicyAcceptsBalancedDefaults()
    {
        GenerationWorkEstimate estimate = GenerationWorkEstimator.Estimate(
            GenerativeMode.Hybrid,
            new ImageSize(1024, 1024),
            new FlowPainterSettings(),
            new PrimitiveGenerationSettings(),
            new HybridGenerationSettings());

        WorkloadBudgetPolicy.EnsureGenerationWithinBudget(estimate);
    }

    [Fact]
    public void MemoryPolicyAcceptsLimitAndRejectsNextByte()
    {
        Assert.True(WorkloadBudgetPolicy.IsMemoryWithinBudget(
            WorkloadBudgetPolicy.MaximumPeakWorkingSetBytes));
        Assert.False(WorkloadBudgetPolicy.IsMemoryWithinBudget(
            WorkloadBudgetPolicy.MaximumPeakWorkingSetBytes + 1L));
        Assert.Throws<InvalidOperationException>(() => WorkloadBudgetPolicy.EnsureMemoryWithinBudget(
            WorkloadBudgetPolicy.MaximumPeakWorkingSetBytes + 1L,
            "Test operation"));
        Assert.Throws<ArgumentOutOfRangeException>(() => WorkloadBudgetPolicy.EnsureGenerationWithinBudget(
            new GenerationWorkEstimate(GenerativeMode.FlowPainting, -1L, 0L, 0L)));
        Assert.Throws<ArgumentOutOfRangeException>(() => WorkloadBudgetPolicy.EnsureGenerationWithinBudget(
            new GenerationWorkEstimate((GenerativeMode)int.MaxValue, 0L, 0L, 0L)));
    }
}
