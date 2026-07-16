using FlowPainter.Application.PrimitiveGeneration;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Primitives;

namespace FlowPainter.Application.Tests.PrimitiveGeneration;

public sealed class DefaultPrimitiveScorerTests
{
    [Fact]
    public void ScoreFindsColorThatImprovesCurrentCanvas()
    {
        ImageSize size = new(8, 8);
        Rgba32[] source = Enumerable.Repeat(Rgba32.Opaque(240, 20, 10), 64).ToArray();
        Rgba32[] current = Enumerable.Repeat(Rgba32.Opaque(0, 0, 0), 64).ToArray();
        GeometricPrimitive candidate = CreateFullRectangle();
        DefaultPrimitiveScorer scorer = new(new PrimitiveMaskRasterizer());

        PrimitiveScore score = scorer.Score(
            size,
            source,
            current,
            detailMap: null,
            candidate,
            new PrimitiveGenerationSettings(opacity: 1d));

        Assert.True(score.Improvement > 0d);
        Assert.Equal(Rgba32.Opaque(240, 20, 10), score.Primitive.Color);
        Assert.Equal(64, score.Mask.PixelIndices.Count);
    }

    [Fact]
    public void ScoreWeightsDetailedPixelsMoreStrongly()
    {
        ImageSize size = new(8, 8);
        Rgba32[] source = Enumerable.Repeat(Rgba32.Opaque(255, 255, 255), 64).ToArray();
        Rgba32[] current = Enumerable.Repeat(Rgba32.Opaque(0, 0, 0), 64).ToArray();
        GeometricPrimitive candidate = CreateFullRectangle();
        DefaultPrimitiveScorer scorer = new(new PrimitiveMaskRasterizer());
        PrimitiveGenerationSettings settings = new(opacity: 1d, detailErrorWeight: 3d);

        PrimitiveScore unweighted = scorer.Score(size, source, current, null, candidate, settings);
        PrimitiveScore weighted = scorer.Score(
            size,
            source,
            current,
            DetailMap.CreateUniform(size, 1f),
            candidate,
            settings);

        Assert.True(weighted.Improvement > unweighted.Improvement);
    }

    [Fact]
    public void ScoreRejectsMismatchedPixelBuffer()
    {
        DefaultPrimitiveScorer scorer = new(new PrimitiveMaskRasterizer());

        Assert.Throws<ArgumentException>(() => scorer.Score(
            new ImageSize(2, 2),
            new Rgba32[3],
            new Rgba32[4],
            null,
            CreateFullRectangle(),
            new PrimitiveGenerationSettings()));
    }

    private static GeometricPrimitive CreateFullRectangle()
    {
        return new GeometricPrimitive(
            0,
            PrimitiveKind.Rectangle,
            new NormalizedPoint(0.5d, 0.5d),
            1d,
            1d,
            0d,
            Rgba32.Opaque(0, 0, 0));
    }
}
