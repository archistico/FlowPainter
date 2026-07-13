using FlowPainter.Application.Generation;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Generation;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.Generation;

public sealed class GenerationRequestTests
{
    [Fact]
    public void ConstructorCopiesDetailRegionCollection()
    {
        List<DetailRegion> regions = [CreateRegion("face")];
        GenerationRequest request = new(
            new ImageSize(2_000, 1_000),
            new ImageSize(4_000, 2_000),
            42UL,
            GenerativeMode.Hybrid,
            regions);

        regions.Clear();

        Assert.Single(request.DetailRegions);
        Assert.Equal(GenerativeMode.Hybrid, request.Mode);
    }

    [Fact]
    public void ConstructorRejectsDuplicateRegionIdentifiersIgnoringCase()
    {
        DetailRegion[] regions = [CreateRegion("Face"), CreateRegion("face")];

        Assert.Throws<ArgumentException>(() => new GenerationRequest(
            new ImageSize(100, 100),
            new ImageSize(100, 100),
            1UL,
            GenerativeMode.FlowPainting,
            regions));
    }

    private static DetailRegion CreateRegion(string id)
    {
        return new DetailRegion(
            id,
            new NormalizedRect(0.1d, 0.1d, 0.5d, 0.5d),
            0.8d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.IncreaseDetail);
    }
}
