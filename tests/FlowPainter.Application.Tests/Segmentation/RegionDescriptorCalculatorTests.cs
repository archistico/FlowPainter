using FlowPainter.Application.Segmentation;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Tests.Segmentation;

public sealed class RegionDescriptorCalculatorTests
{
    private static readonly uint[] VerticalSplitLabels =
    [
        0, 0, 1, 1,
        0, 0, 1, 1,
    ];

    private static readonly uint[] SingleRegionTwoPixelLabels = [0, 0];
    private static readonly uint[] SeparateTwoPixelLabels = [0, 1];
    private static readonly uint[] SingleRegionThreePixelLabels = [0, 0, 0];
    private static readonly uint[] SinglePixelLabels = [0];
    private static readonly uint[] SingleRegionFourPixelLabels = new uint[4];
    private static readonly uint[] SingleRegionNinePixelLabels = new uint[9];
    private static readonly uint[] SingleRegionSixteenPixelLabels = new uint[16];
    private static readonly uint[] SteppedLabels = [0, 0, 0, 1];
    private static readonly Rgba32[] BlackWhitePixels =
    [
        Rgba32.Opaque(0, 0, 0),
        Rgba32.Opaque(255, 255, 255),
    ];
    private static readonly Rgba32[] TransparentPixels = [new Rgba32(0, 0, 0, 0)];
    private static readonly Rgba32[] LightnessGradientPixels =
    [
        Rgba32.Opaque(0, 0, 0),
        Rgba32.Opaque(128, 128, 128),
        Rgba32.Opaque(255, 255, 255),
    ];

    [Fact]
    public void CalculateBuildsKnownAreaBoundsCentroidAndPerimeter()
    {
        RgbaImage source = CreateUniformImage(4, 2, Rgba32.Opaque(80, 90, 100));
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 2, VerticalSplitLabels);

        ImageRegion[] regions = RegionDescriptorCalculator.Calculate(source, labels);

        Assert.Equal(2, regions.Length);
        Assert.Equal(4, regions[0].PixelCount);
        Assert.Equal(0.5d, regions[0].NormalizedArea);
        Assert.Equal(new PixelBounds(0, 0, 2, 2), regions[0].Bounds);
        Assert.Equal(new RegionCentroid(1d, 1d), regions[0].Centroid);
        Assert.Equal(8d, regions[0].Descriptors.Perimeter);
        Assert.Equal(Math.PI / 4d, regions[0].Descriptors.Compactness, 12);
        Assert.Equal(new PixelBounds(2, 0, 4, 2), regions[1].Bounds);
        Assert.Equal(new RegionCentroid(3d, 1d), regions[1].Centroid);
    }

    [Fact]
    public void CalculateMeasuresSteppedDigitalPerimeter()
    {
        RgbaImage source = CreateUniformImage(2, 2, Rgba32.Opaque(80, 90, 100));
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 2, SteppedLabels);

        ImageRegion[] regions = RegionDescriptorCalculator.Calculate(source, labels);

        Assert.Equal(8d, regions[0].Descriptors.Perimeter);
        Assert.Equal(3d * Math.PI / 16d, regions[0].Descriptors.Compactness, 12);
        Assert.Equal(4d, regions[1].Descriptors.Perimeter);
        Assert.Equal(Math.PI / 4d, regions[1].Descriptors.Compactness, 12);
    }

    [Fact]
    public void CalculateProducesAnalyticalBlackWhiteLabStatistics()
    {
        RgbaImage source = new(new ImageSize(2, 1), BlackWhitePixels);
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 1, SingleRegionTwoPixelLabels);

        RegionVisualDescriptors descriptors = RegionDescriptorCalculator.Calculate(source, labels)[0].Descriptors;

        Assert.Equal(50d, descriptors.MeanLightness, 5);
        Assert.Equal(2500d, descriptors.LightnessVariance, 3);
        Assert.InRange(descriptors.MeanA, -0.001d, 0.001d);
        Assert.InRange(descriptors.MeanB, -0.001d, 0.001d);
        Assert.InRange(descriptors.AVariance, 0d, 0.001d);
        Assert.InRange(descriptors.BVariance, 0d, 0.001d);
    }

    [Fact]
    public void CalculateCompositesTransparentPixelsOnWhite()
    {
        RgbaImage source = new(new ImageSize(1, 1), TransparentPixels);
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 1, SinglePixelLabels);

        RegionVisualDescriptors descriptors = RegionDescriptorCalculator.Calculate(source, labels)[0].Descriptors;

        Assert.Equal(100d, descriptors.MeanLightness, 5);
        Assert.InRange(descriptors.MeanA, -0.001d, 0.001d);
        Assert.InRange(descriptors.MeanB, -0.001d, 0.001d);
    }

    [Fact]
    public void CalculateReturnsZeroTextureForUniformRegion()
    {
        RgbaImage source = CreateUniformImage(3, 3, Rgba32.Opaque(120, 120, 120));
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 1, SingleRegionNinePixelLabels);

        RegionVisualDescriptors descriptors = RegionDescriptorCalculator.Calculate(source, labels)[0].Descriptors;

        Assert.Equal(0d, descriptors.TextureEnergy);
        Assert.Equal(0d, descriptors.EdgeDensity);
        Assert.Equal(0d, descriptors.DominantOrientationRadians);
    }

    [Fact]
    public void CalculateDoesNotTreatInterRegionContrastAsTexture()
    {
        RgbaImage source = new(new ImageSize(2, 1), BlackWhitePixels);
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 2, SeparateTwoPixelLabels);

        ImageRegion[] regions = RegionDescriptorCalculator.Calculate(source, labels);

        Assert.All(regions, region => Assert.Equal(0d, region.Descriptors.TextureEnergy));
        Assert.All(regions, region => Assert.Equal(0d, region.Descriptors.EdgeDensity));
    }

    [Fact]
    public void CalculateFindsVerticalTangentForHorizontalLightnessGradient()
    {
        RgbaImage source = new(new ImageSize(3, 1), LightnessGradientPixels);
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 1, SingleRegionThreePixelLabels);

        RegionVisualDescriptors descriptors = RegionDescriptorCalculator.Calculate(source, labels)[0].Descriptors;

        Assert.True(descriptors.TextureEnergy > 0d);
        Assert.Equal(1d, descriptors.EdgeDensity);
        Assert.Equal(Math.PI / 2d, descriptors.DominantOrientationRadians, 12);
    }

    [Fact]
    public void CalculateFindsHorizontalTangentForVerticalLightnessGradient()
    {
        RgbaImage source = new(new ImageSize(1, 3), LightnessGradientPixels);
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 1, SingleRegionThreePixelLabels);

        RegionVisualDescriptors descriptors = RegionDescriptorCalculator.Calculate(source, labels)[0].Descriptors;

        Assert.True(descriptors.TextureEnergy > 0d);
        Assert.Equal(1d, descriptors.EdgeDensity);
        Assert.Equal(0d, descriptors.DominantOrientationRadians, 12);
    }

    [Fact]
    public void CalculateDistinguishesUniformAndTexturedRegions()
    {
        RgbaImage uniform = CreateUniformImage(4, 4, Rgba32.Opaque(128, 128, 128));
        RgbaImage textured = new(
            new ImageSize(4, 4),
            Enumerable.Range(0, 16)
                .Select(index => index % 2 == 0
                    ? Rgba32.Opaque(20, 20, 20)
                    : Rgba32.Opaque(235, 235, 235))
                .ToArray());
        RegionLabelMap labels = RegionLabelMap.Create(uniform.Size, 1, SingleRegionSixteenPixelLabels);

        double uniformEnergy = RegionDescriptorCalculator.Calculate(uniform, labels)[0].Descriptors.TextureEnergy;
        RegionVisualDescriptors texturedDescriptors = RegionDescriptorCalculator.Calculate(textured, labels)[0].Descriptors;

        Assert.Equal(0d, uniformEnergy);
        Assert.True(texturedDescriptors.TextureEnergy > uniformEnergy);
        Assert.True(texturedDescriptors.EdgeDensity > 0d);
    }

    [Fact]
    public void CalculateIsDeterministic()
    {
        RgbaImage source = CreateUniformImage(4, 2, Rgba32.Opaque(70, 100, 140));
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 2, VerticalSplitLabels);

        ImageRegion[] first = RegionDescriptorCalculator.Calculate(source, labels);
        ImageRegion[] second = RegionDescriptorCalculator.Calculate(source, labels);

        for (int regionId = 0; regionId < first.Length; regionId++)
        {
            Assert.Equal(first[regionId].Bounds, second[regionId].Bounds);
            Assert.Equal(first[regionId].Centroid, second[regionId].Centroid);
            Assert.Equal(
                first[regionId].Descriptors.MeanLightness,
                second[regionId].Descriptors.MeanLightness);
            Assert.Equal(
                first[regionId].Descriptors.TextureEnergy,
                second[regionId].Descriptors.TextureEnergy);
        }
    }

    [Fact]
    public void CalculateDoesNotMutateSource()
    {
        RgbaImage source = CreateUniformImage(4, 2, Rgba32.Opaque(70, 100, 140));
        Rgba32[] before = source.CopyPixels();
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 2, VerticalSplitLabels);

        _ = RegionDescriptorCalculator.Calculate(source, labels);

        Assert.Equal(before, source.CopyPixels());
    }

    [Fact]
    public void CalculateRejectsDimensionMismatch()
    {
        RgbaImage source = CreateUniformImage(2, 2, Rgba32.Opaque(0, 0, 0));
        RegionLabelMap labels = RegionLabelMap.Create(new ImageSize(1, 1), 1, SinglePixelLabels);

        Assert.Throws<ArgumentException>(() => RegionDescriptorCalculator.Calculate(source, labels));
    }

    [Fact]
    public void CalculateHonorsPreCancelledToken()
    {
        RgbaImage source = CreateUniformImage(2, 2, Rgba32.Opaque(0, 0, 0));
        RegionLabelMap labels = RegionLabelMap.Create(source.Size, 1, SingleRegionFourPixelLabels);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() => RegionDescriptorCalculator.Calculate(
            source,
            labels,
            cancellation.Token));
    }

    private static RgbaImage CreateUniformImage(int width, int height, Rgba32 color)
    {
        Rgba32[] pixels = new Rgba32[checked(width * height)];
        Array.Fill(pixels, color);
        return new RgbaImage(new ImageSize(width, height), pixels);
    }
}
