using FlowPainter.Application.Semantics;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Semantics;

namespace FlowPainter.Application.Tests.Semantics;

public sealed class SemanticCorrectionComposerTests
{
    [Fact]
    public void ApplyReturnsOriginalResultWhenNoCorrectionsExist()
    {
        SemanticAnalysisResult source = CreateResult(0.25f);

        SemanticAnalysisResult result = SemanticCorrectionComposer.Apply(source, [], 0.05d);

        Assert.Same(source, result);
    }

    [Fact]
    public void ForcePrimarySubjectPromotesSubjectFocalAndImportance()
    {
        SemanticAnalysisResult result = SemanticCorrectionComposer.Apply(
            CreateResult(0f),
            [CreateCorrection(SemanticCorrectionKind.ForcePrimarySubject)],
            0d);

        Assert.Equal(1f, result.SubjectMap[2, 2]);
        Assert.Equal(1f, result.FocalMap[2, 2]);
        Assert.Equal(1f, result.ImportanceMap[2, 2]);
        Assert.Equal(0f, result.SubjectMap[0, 0]);
    }

    [Fact]
    public void ForceSubjectDoesNotCreateFocalArea()
    {
        SemanticAnalysisResult result = SemanticCorrectionComposer.Apply(
            CreateResult(0f),
            [CreateCorrection(SemanticCorrectionKind.ForceSubject)],
            0d);

        Assert.Equal(1f, result.SubjectMap[2, 2]);
        Assert.Equal(0f, result.FocalMap[2, 2]);
        Assert.Equal(0.85f, result.ImportanceMap[2, 2], 5);
    }

    [Fact]
    public void ForceBackgroundSuppressesAllSemanticMaps()
    {
        SemanticAnalysisResult result = SemanticCorrectionComposer.Apply(
            CreateResult(1f),
            [CreateCorrection(SemanticCorrectionKind.ForceBackground)],
            0d);

        Assert.Equal(0f, result.SaliencyMap[2, 2]);
        Assert.Equal(0f, result.SubjectMap[2, 2]);
        Assert.Equal(0f, result.SilhouetteMap[2, 2]);
        Assert.Equal(0f, result.FocalMap[2, 2]);
        Assert.Equal(0f, result.ImportanceMap[2, 2]);
    }

    [Fact]
    public void IgnoreAutomaticDetectionPreservesRawSaliency()
    {
        SemanticAnalysisResult result = SemanticCorrectionComposer.Apply(
            CreateResult(1f),
            [CreateCorrection(SemanticCorrectionKind.IgnoreAutomaticDetection)],
            0d);

        Assert.Equal(1f, result.SaliencyMap[2, 2]);
        Assert.Equal(0f, result.SubjectMap[2, 2]);
        Assert.Equal(0f, result.FocalMap[2, 2]);
        Assert.Equal(0f, result.ImportanceMap[2, 2]);
    }

    [Fact]
    public void BackgroundCorrectionHasPrecedenceOverForcedSubject()
    {
        SemanticAnalysisResult result = SemanticCorrectionComposer.Apply(
            CreateResult(0f),
            [
                CreateCorrection(SemanticCorrectionKind.ForceSubject),
                CreateCorrection(SemanticCorrectionKind.ForceBackground)
            ],
            0d);

        Assert.Equal(0f, result.SubjectMap[2, 2]);
        Assert.Equal(0f, result.ImportanceMap[2, 2]);
    }

    [Fact]
    public void PrimarySubjectHasPrecedenceOverBackgroundCorrection()
    {
        SemanticAnalysisResult result = SemanticCorrectionComposer.Apply(
            CreateResult(0f),
            [
                CreateCorrection(SemanticCorrectionKind.ForceBackground),
                CreateCorrection(SemanticCorrectionKind.ForcePrimarySubject)
            ],
            0d);

        Assert.Equal(1f, result.SubjectMap[2, 2]);
        Assert.Equal(1f, result.FocalMap[2, 2]);
        Assert.Equal(1f, result.ImportanceMap[2, 2]);
    }

    [Fact]
    public void ApplyUsesSoftTransitionOutsideCorrectionBounds()
    {
        SemanticCorrectionRegion correction = new(
            "semantic-correction-0001",
            new NormalizedRect(0.4d, 0.4d, 0.6d, 0.6d),
            SemanticCorrectionKind.ForceSubject);

        SemanticAnalysisResult result = SemanticCorrectionComposer.Apply(
            CreateResult(0f, 20, 20),
            [correction],
            0.2d);

        Assert.True(result.SubjectMap[7, 10] > 0f);
        Assert.True(result.SubjectMap[7, 10] < result.SubjectMap[10, 10]);
        Assert.Equal(0f, result.SubjectMap[0, 0]);
    }

    [Theory]
    [InlineData(-0.1d)]
    [InlineData(0.6d)]
    [InlineData(double.NaN)]
    public void ApplyRejectsInvalidTransitionWidth(double transitionWidth)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SemanticCorrectionComposer.Apply(
            CreateResult(0f),
            [CreateCorrection(SemanticCorrectionKind.ForceSubject)],
            transitionWidth));
    }

    private static SemanticAnalysisResult CreateResult(float value, int width = 5, int height = 5)
    {
        DetailMap map = DetailMap.CreateUniform(new ImageSize(width, height), value);
        return new SemanticAnalysisResult(map, map, map, map, map);
    }

    private static SemanticCorrectionRegion CreateCorrection(SemanticCorrectionKind kind)
    {
        return new SemanticCorrectionRegion(
            $"correction-{kind}",
            new NormalizedRect(0.2d, 0.2d, 0.8d, 0.8d),
            kind);
    }
}
