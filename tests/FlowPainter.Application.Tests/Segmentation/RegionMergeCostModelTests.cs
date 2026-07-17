using FlowPainter.Application.Segmentation;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Tests.Segmentation;

public sealed class RegionMergeCostModelTests
{
    [Fact]
    public void CalculateRanksStrongBoundaryAboveWeakBoundary()
    {
        ImageRegion first = CreateRegion(0, 10d, 2d);
        ImageRegion second = CreateRegion(1, 10d, 2d);
        RegionAdjacency weak = new(0, 1, 4, boundaryStrength: 0.1d);
        RegionAdjacency strong = new(0, 1, 4, boundaryStrength: 0.8d);

        double weakCost = RegionMergeCostModel.Calculate(first, second, weak, 1d);
        double strongCost = RegionMergeCostModel.Calculate(first, second, strong, 1d);

        Assert.True(strongCost > weakCost);
    }

    [Fact]
    public void CalculateRanksColorContrastAboveMatchingColor()
    {
        ImageRegion first = CreateRegion(0, 10d, 2d);
        ImageRegion matching = CreateRegion(1, 10d, 2d);
        ImageRegion contrasting = CreateRegion(1, 90d, 2d);
        RegionAdjacency boundary = new(0, 1, 4);

        double matchingCost = RegionMergeCostModel.Calculate(first, matching, boundary, 1d);
        double contrastingCost = RegionMergeCostModel.Calculate(first, contrasting, boundary, 1d);

        Assert.True(contrastingCost > matchingCost);
    }

    [Fact]
    public void CalculateIncludesTextureAndResultingSizePenalties()
    {
        ImageRegion first = CreateRegion(0, 10d, 1d, normalizedArea: 0.2d);
        ImageRegion smooth = CreateRegion(1, 10d, 1d, normalizedArea: 0.2d);
        ImageRegion textured = CreateRegion(1, 10d, 30d, normalizedArea: 0.2d);
        RegionAdjacency boundary = new(0, 1, 4);

        double smoothCost = RegionMergeCostModel.Calculate(first, smooth, boundary, 1d);
        double texturedCost = RegionMergeCostModel.Calculate(first, textured, boundary, 1d);
        double constrainedCost = RegionMergeCostModel.Calculate(first, smooth, boundary, 0.4d);

        Assert.True(texturedCost > smoothCost);
        Assert.True(constrainedCost > smoothCost);
    }

    [Fact]
    public void CalculateRejectsBoundaryForDifferentRegions()
    {
        ImageRegion first = CreateRegion(0, 10d, 2d);
        ImageRegion second = CreateRegion(1, 10d, 2d);
        RegionAdjacency boundary = new(1, 2, 4);

        Assert.Throws<ArgumentException>(() => RegionMergeCostModel.Calculate(
            first,
            second,
            boundary,
            1d));
    }

    private static ImageRegion CreateRegion(
        int id,
        double meanLightness,
        double textureEnergy,
        double normalizedArea = 0.25d)
    {
        return new ImageRegion(
            id,
            25,
            normalizedArea,
            new PixelBounds(id, 0, id + 1, 1),
            new RegionCentroid(id + 0.5d, 0.5d),
            new RegionVisualDescriptors(
                perimeter: 20d,
                compactness: 0.7d,
                meanLightness: meanLightness,
                textureEnergy: textureEnergy));
    }
}
