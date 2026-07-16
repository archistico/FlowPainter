using FlowPainter.Application.Segmentation;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.Segmentation;

public sealed class RegionSegmentationRequestTests
{
    [Fact]
    public void ConstructorPreservesSourceSettingsAndRevisions()
    {
        RgbaImage source = CreateSource();
        RegionSegmentationSettings settings = new();

        RegionSegmentationRequest request = new(source, settings, 4, 7);

        Assert.Same(source, request.Source);
        Assert.Same(settings, request.Settings);
        Assert.Equal(4, request.SourceRevision);
        Assert.Equal(7, request.SettingsRevision);
    }

    [Fact]
    public void ConstructorRejectsNullDependencies()
    {
        RgbaImage source = CreateSource();

        Assert.Throws<ArgumentNullException>(() => new RegionSegmentationRequest(null!, new RegionSegmentationSettings()));
        Assert.Throws<ArgumentNullException>(() => new RegionSegmentationRequest(source, null!));
    }

    [Fact]
    public void ConstructorRejectsNegativeRevisions()
    {
        RgbaImage source = CreateSource();
        RegionSegmentationSettings settings = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionSegmentationRequest(source, settings, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionSegmentationRequest(source, settings, 0, -1));
    }

    private static RgbaImage CreateSource()
    {
        return new RgbaImage(new ImageSize(1, 1), new[] { new Rgba32(1, 2, 3, 255) });
    }
}
