using FlowPainter.Application.Boundaries;
using FlowPainter.Domain.Boundaries;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.Boundaries;

public sealed class SceneBoundaryAnalysisResultTests
{
    [Fact]
    public void ConstructorAcceptsMapsWithMatchingDimensions()
    {
        ImageSize size = new(3, 2);
        DetailMap map = DetailMap.CreateUniform(size, 0.5f);

        SceneBoundaryAnalysisResult result = new(
            map,
            map,
            map,
            map,
            map,
            map,
            map,
            BoundaryDirectionField.CreateEmpty(size),
            "test-provider");

        Assert.Equal(size, result.EdgeImportanceMap.Size);
        Assert.Equal("test-provider", result.ProviderId);
    }

    [Fact]
    public void ConstructorRejectsMismatchedDimensions()
    {
        DetailMap small = DetailMap.CreateUniform(new ImageSize(2, 2), 0f);
        DetailMap large = DetailMap.CreateUniform(new ImageSize(3, 2), 0f);

        Assert.Throws<ArgumentException>(() => new SceneBoundaryAnalysisResult(
            small,
            large,
            small,
            small,
            small,
            small,
            small,
            BoundaryDirectionField.CreateEmpty(new ImageSize(2, 2))));
    }

    [Fact]
    public void CreateEmptyReturnsZeroMapsAndUndefinedDirections()
    {
        SceneBoundaryAnalysisResult result = SceneBoundaryAnalysisResult.CreateEmpty(new ImageSize(4, 3));

        Assert.All(result.EdgeImportanceMap.CopyValues(), value => Assert.Equal(0f, value));
        Assert.All(result.DirectionField.CopyVectors(), vector => Assert.False(vector.IsDefined));
    }
}
