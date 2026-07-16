using FlowPainter.Application.Background;
using FlowPainter.Application.Boundaries;
using FlowPainter.Application.Semantics;
using FlowPainter.Domain.Boundaries;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.Background;

public sealed class BackgroundSuppressionComposerTests
{
    private static readonly ImageSize MapSize = new(2, 2);

    [Fact]
    public void DisabledSettingsPreserveComposedDetailMap()
    {
        DetailMap detail = new(2, 2, [0.2f, 0.4f, 0.6f, 0.8f]);

        BackgroundSuppressionResult result = BackgroundSuppressionComposer.Compose(
            detail,
            detail,
            SemanticAnalysisResult.CreateEmpty(MapSize),
            SceneBoundaryAnalysisResult.CreateEmpty(MapSize),
            new BackgroundSuppressionSettings());

        Assert.Equal(detail.CopyValues(), result.EffectiveDetailMap.CopyValues());
        Assert.All(result.SuppressionMap.CopyValues(), value => Assert.Equal(0f, value));
    }

    [Fact]
    public void UniformBackgroundIsSuppressedToConfiguredFloor()
    {
        DetailMap detail = DetailMap.CreateUniform(MapSize, 0.8f);
        SceneBoundaryAnalysisResult boundaries = CreateBoundaries(
            background: [1f, 1f, 1f, 1f],
            uncertainty: [0f, 0f, 0f, 0f],
            subjectBoundary: [0f, 0f, 0f, 0f]);
        BackgroundSuppressionSettings settings = new(
            enabled: true,
            overallStrength: 1d,
            detailFloor: 0.2d,
            uncertaintyProtection: 0d,
            silhouetteProtection: 0d,
            transitionSoftness: 0d);

        BackgroundSuppressionResult result = BackgroundSuppressionComposer.Compose(
            detail,
            detail,
            SemanticAnalysisResult.CreateEmpty(MapSize),
            boundaries,
            settings);

        Assert.All(result.SuppressionMap.CopyValues(), value => Assert.Equal(1f, value));
        Assert.All(result.EffectiveDetailMap.CopyValues(), value => Assert.Equal(0.2f, value));
        Assert.All(result.ArtisticDetailField.CopyValues(), value => Assert.Equal(-1f, value));
    }

    [Fact]
    public void SemanticSubjectProtectsBackgroundFromSuppression()
    {
        DetailMap detail = DetailMap.CreateUniform(MapSize, 0.6f);
        DetailMap subject = new(2, 2, [1f, 0f, 0f, 0f]);
        DetailMap empty = DetailMap.CreateUniform(MapSize, 0f);
        SemanticAnalysisResult semantics = new(empty, subject, empty, empty, subject);
        SceneBoundaryAnalysisResult boundaries = CreateBoundaries(
            background: [1f, 1f, 1f, 1f],
            uncertainty: [0f, 0f, 0f, 0f],
            subjectBoundary: [0f, 0f, 0f, 0f]);

        BackgroundSuppressionResult result = BackgroundSuppressionComposer.Compose(
            detail,
            detail,
            semantics,
            boundaries,
            EnabledSettings());

        Assert.Equal(0f, result.SuppressionMap[0, 0]);
        Assert.Equal(0.6f, result.ArtisticDetailField[0, 0]);
        Assert.True(result.SuppressionMap[1, 0] > 0.9f);
        Assert.True(result.ArtisticDetailField[1, 0] < -0.9f);
    }

    [Fact]
    public void UncertainAreaIsProtectedAccordingToConfiguredWeight()
    {
        DetailMap detail = DetailMap.CreateUniform(MapSize, 0.7f);
        SceneBoundaryAnalysisResult boundaries = CreateBoundaries(
            background: [1f, 1f, 1f, 1f],
            uncertainty: [1f, 0f, 0f, 0f],
            subjectBoundary: [0f, 0f, 0f, 0f]);

        BackgroundSuppressionResult result = BackgroundSuppressionComposer.Compose(
            detail,
            detail,
            SemanticAnalysisResult.CreateEmpty(MapSize),
            boundaries,
            new BackgroundSuppressionSettings(
                enabled: true,
                overallStrength: 1d,
                uncertaintyProtection: 1d,
                silhouetteProtection: 0d,
                transitionSoftness: 0d));

        Assert.Equal(0f, result.SuppressionMap[0, 0]);
        Assert.Equal(1f, result.SuppressionMap[1, 0]);
    }

    [Fact]
    public void ManualDetailIncreaseHasPriorityOverAutomaticBackground()
    {
        DetailMap automatic = DetailMap.CreateUniform(MapSize, 0.2f);
        DetailMap composed = new(2, 2, [1f, 0.2f, 0.2f, 0.2f]);
        SceneBoundaryAnalysisResult boundaries = CreateBoundaries(
            background: [1f, 1f, 1f, 1f],
            uncertainty: [0f, 0f, 0f, 0f],
            subjectBoundary: [0f, 0f, 0f, 0f]);

        BackgroundSuppressionResult result = BackgroundSuppressionComposer.Compose(
            automatic,
            composed,
            SemanticAnalysisResult.CreateEmpty(MapSize),
            boundaries,
            EnabledSettings());

        Assert.True(result.ProtectionMap[0, 0] >= 0.8f);
        Assert.True(result.SuppressionMap[0, 0] < result.SuppressionMap[1, 0]);
    }

    [Fact]
    public void SmoothingDoesNotReintroduceSuppressionIntoFullyProtectedSubject()
    {
        DetailMap detail = DetailMap.CreateUniform(MapSize, 0.6f);
        DetailMap subject = new(2, 2, [1f, 0f, 0f, 0f]);
        DetailMap empty = DetailMap.CreateUniform(MapSize, 0f);
        SemanticAnalysisResult semantics = new(empty, subject, empty, empty, subject);
        SceneBoundaryAnalysisResult boundaries = CreateBoundaries(
            background: [1f, 1f, 1f, 1f],
            uncertainty: [0f, 0f, 0f, 0f],
            subjectBoundary: [0f, 0f, 0f, 0f]);

        BackgroundSuppressionResult result = BackgroundSuppressionComposer.Compose(
            detail,
            detail,
            semantics,
            boundaries,
            new BackgroundSuppressionSettings(
                enabled: true,
                overallStrength: 1d,
                transitionSoftness: 1d));

        Assert.Equal(0f, result.SuppressionMap[0, 0]);
    }

    [Fact]
    public void ComposeHonorsCancellation()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() => BackgroundSuppressionComposer.Compose(
            DetailMap.CreateUniform(MapSize, 0.5f),
            DetailMap.CreateUniform(MapSize, 0.5f),
            SemanticAnalysisResult.CreateEmpty(MapSize),
            SceneBoundaryAnalysisResult.CreateEmpty(MapSize),
            new BackgroundSuppressionSettings(enabled: true),
            cancellationToken: cancellation.Token));
    }

    private static BackgroundSuppressionSettings EnabledSettings()
    {
        return new BackgroundSuppressionSettings(
            enabled: true,
            overallStrength: 1d,
            detailFloor: 0.1d,
            uncertaintyProtection: 1d,
            silhouetteProtection: 1d,
            transitionSoftness: 0d);
    }

    private static SceneBoundaryAnalysisResult CreateBoundaries(
        float[] background,
        float[] uncertainty,
        float[] subjectBoundary)
    {
        DetailMap empty = DetailMap.CreateUniform(MapSize, 0f);
        return new SceneBoundaryAnalysisResult(
            empty,
            empty,
            new DetailMap(2, 2, subjectBoundary),
            empty,
            empty,
            new DetailMap(2, 2, background),
            new DetailMap(2, 2, uncertainty),
            BoundaryDirectionField.CreateEmpty(MapSize));
    }
}
