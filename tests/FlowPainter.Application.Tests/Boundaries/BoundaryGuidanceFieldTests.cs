using FlowPainter.Application.Boundaries;
using FlowPainter.Domain.Boundaries;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.Boundaries;

public sealed class BoundaryGuidanceFieldTests
{
    [Fact]
    public void CreatePromotesSubjectBoundaryOverTexture()
    {
        SceneBoundaryAnalysisResult analysis = CreateAnalysis(
            edgeImportance: [0f, 0f, 0f],
            subjectBoundary: [0f, 1f, 0f],
            internalStructure: [0f, 0f, 0f],
            textureEdges: [1f, 0f, 1f],
            directions:
            [
                new BoundaryVector(1d, 0d),
                new BoundaryVector(0d, 1d),
                new BoundaryVector(1d, 0d)
            ]);
        BoundaryPaintingSettings settings = new(
            enabled: true,
            alignmentRadius: 0,
            textureEdgeInfluence: 0.1d);

        BoundaryGuidanceField field = BoundaryGuidanceField.Create(analysis, settings);
        BoundaryGuidanceSample subject = field.SampleNearest(new NormalizedPoint(0.5d, 0.5d));
        BoundaryGuidanceSample texture = field.SampleNearest(new NormalizedPoint(0.1d, 0.5d));

        Assert.True(subject.Influence > texture.Influence);
        Assert.True(subject.Hardness > texture.Hardness);
        Assert.Equal(1d, subject.SubjectBoundary, 12);
    }

    [Fact]
    public void CreatePropagatesTangentWithinAlignmentRadius()
    {
        SceneBoundaryAnalysisResult analysis = CreateAnalysis(
            edgeImportance: [0f, 1f, 0f, 0f, 0f],
            subjectBoundary: [0f, 1f, 0f, 0f, 0f],
            internalStructure: [0f, 0f, 0f, 0f, 0f],
            textureEdges: [0f, 0f, 0f, 0f, 0f],
            directions:
            [
                default,
                new BoundaryVector(0d, 1d),
                default,
                default,
                default
            ]);

        BoundaryGuidanceField field = BoundaryGuidanceField.Create(
            analysis,
            new BoundaryPaintingSettings(enabled: true, alignmentRadius: 2));
        BoundaryGuidanceSample adjacent = field.SampleNearest(new NormalizedPoint(0.5d, 0.5d));
        BoundaryGuidanceSample distant = field.SampleNearest(new NormalizedPoint(0.9d, 0.5d));

        Assert.True(adjacent.HasDirection);
        Assert.True(adjacent.Influence > distant.Influence);
        Assert.True(Math.Abs(adjacent.Tangent.Y) > 0.9d);
    }

    [Fact]
    public void CreateReinforcedDetailMapRaisesContourWithoutMutatingSource()
    {
        SceneBoundaryAnalysisResult analysis = CreateAnalysis(
            edgeImportance: [0f, 1f, 0f],
            subjectBoundary: [0f, 1f, 0f],
            internalStructure: [0f, 0f, 0f],
            textureEdges: [0f, 0f, 0f],
            directions: [default, new BoundaryVector(0d, 1d), default]);
        BoundaryGuidanceField field = BoundaryGuidanceField.Create(
            analysis,
            new BoundaryPaintingSettings(enabled: true, alignmentRadius: 0));
        DetailMap source = DetailMap.CreateUniform(new ImageSize(3, 1), 0.2f);

        DetailMap reinforced = field.CreateReinforcedDetailMap(source, 0.75d);

        Assert.Equal(0.2f, source[1, 0]);
        Assert.True(reinforced[1, 0] > reinforced[0, 0]);
        Assert.Equal(0.2f, reinforced[0, 0]);
    }

    [Fact]
    public void CreateDetectsDirectionChangesAsCorners()
    {
        SceneBoundaryAnalysisResult analysis = CreateAnalysis(
            edgeImportance: [1f, 1f, 1f],
            subjectBoundary: [1f, 1f, 1f],
            internalStructure: [0f, 0f, 0f],
            textureEdges: [0f, 0f, 0f],
            directions:
            [
                new BoundaryVector(1d, 0d),
                new BoundaryVector(1d, 0d),
                new BoundaryVector(0d, 1d)
            ]);

        BoundaryGuidanceField field = BoundaryGuidanceField.Create(
            analysis,
            new BoundaryPaintingSettings(enabled: true, alignmentRadius: 0));

        Assert.True(field.SampleNearest(new NormalizedPoint(0.5d, 0.5d)).CornerStrength > 0.5d);
    }

    [Fact]
    public void CreateHonorsPreCancelledToken()
    {
        SceneBoundaryAnalysisResult analysis = CreateAnalysis(
            edgeImportance: [1f],
            subjectBoundary: [1f],
            internalStructure: [0f],
            textureEdges: [0f],
            directions: [new BoundaryVector(1d, 0d)]);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() => BoundaryGuidanceField.Create(
            analysis,
            new BoundaryPaintingSettings(enabled: true),
            cancellation.Token));
    }

    private static SceneBoundaryAnalysisResult CreateAnalysis(
        ReadOnlySpan<float> edgeImportance,
        ReadOnlySpan<float> subjectBoundary,
        ReadOnlySpan<float> internalStructure,
        ReadOnlySpan<float> textureEdges,
        ReadOnlySpan<BoundaryVector> directions)
    {
        ImageSize size = new(edgeImportance.Length, 1);
        DetailMap empty = DetailMap.CreateUniform(size, 0f);
        return new SceneBoundaryAnalysisResult(
            new DetailMap(size.Width, size.Height, edgeImportance.ToArray()),
            new DetailMap(size.Width, size.Height, edgeImportance.ToArray()),
            new DetailMap(size.Width, size.Height, subjectBoundary.ToArray()),
            new DetailMap(size.Width, size.Height, internalStructure.ToArray()),
            new DetailMap(size.Width, size.Height, textureEdges.ToArray()),
            empty,
            empty,
            new BoundaryDirectionField(size.Width, size.Height, directions.ToArray()),
            "test-boundaries");
    }
}
