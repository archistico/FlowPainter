using FlowPainter.Application.Semantics;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.Semantics;

public sealed class SemanticDetailMapComposerTests
{
    private static readonly ImageSize MapSize = new(2, 2);

    [Fact]
    public void CombineBoostsStructuralMapWithoutExceedingOne()
    {
        float[] structuralValues = [0.2f, 0.5f, 0.9f, 1f];
        float[] semanticValues = [1f, 0.5f, 1f, 1f];
        DetailMap structural = new(2, 2, structuralValues);
        SemanticAnalysisResult semantic = CreateResult(semanticValues);

        DetailMap combined = SemanticDetailMapComposer.Combine(
            structural,
            semantic,
            new SemanticAnalysisSettings(overallInfluence: 0.5d));

        Assert.InRange(combined[0, 0], 0.599999f, 0.600001f);
        Assert.InRange(combined[1, 0], 0.624999f, 0.625001f);
        Assert.InRange(combined[0, 1], 0.949999f, 0.950001f);
        Assert.InRange(combined[1, 1], 0.999999f, 1f);
    }

    [Fact]
    public void CombineWithDisabledAnalysisReturnsIndependentCopy()
    {
        float[] structuralValues = [0.1f, 0.2f, 0.3f, 0.4f];
        float[] semanticValues = [1f, 1f, 1f, 1f];
        DetailMap structural = new(2, 2, structuralValues);
        SemanticAnalysisResult semantic = CreateResult(semanticValues);

        DetailMap combined = SemanticDetailMapComposer.Combine(
            structural,
            semantic,
            new SemanticAnalysisSettings(enabled: false));

        Assert.NotSame(structural, combined);
        Assert.Equal(structural.CopyValues(), combined.CopyValues());
    }

    [Fact]
    public void CombineRejectsDifferentDimensions()
    {
        DetailMap structural = DetailMap.CreateUniform(MapSize, 0f);
        SemanticAnalysisResult semantic = SemanticAnalysisResult.CreateEmpty(new ImageSize(3, 2));

        Assert.Throws<ArgumentException>(
            () => SemanticDetailMapComposer.Combine(
                structural,
                semantic,
                new SemanticAnalysisSettings()));
    }

    [Fact]
    public void CombineHonorsPreCancelledToken()
    {
        DetailMap structural = DetailMap.CreateUniform(new ImageSize(100, 100), 0f);
        SemanticAnalysisResult semantic = SemanticAnalysisResult.CreateEmpty(new ImageSize(100, 100));
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(
            () => SemanticDetailMapComposer.Combine(
                structural,
                semantic,
                new SemanticAnalysisSettings(),
                cancellation.Token));
    }

    private static SemanticAnalysisResult CreateResult(float[] importance)
    {
        DetailMap empty = DetailMap.CreateUniform(MapSize, 0f);
        DetailMap importanceMap = new(MapSize.Width, MapSize.Height, importance);
        return new SemanticAnalysisResult(empty, empty, empty, empty, importanceMap);
    }
}
