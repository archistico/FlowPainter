using FlowPainter.Domain.Images;

namespace FlowPainter.Domain.Tests.Images;

public sealed class ImageSizeTests
{
    [Fact]
    public void ConstructorAcceptsMaximumSupportedDimensions()
    {
        ImageSize size = new(10_000, 10_000);

        Assert.Equal(100_000_000L, size.PixelCount);
        Assert.Equal(400_000_000L, size.GetRequiredBytes(ImageSize.RgbaBytesPerPixel));
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(100, 0)]
    [InlineData(-1, 100)]
    [InlineData(100, -1)]
    [InlineData(10_001, 100)]
    [InlineData(100, 10_001)]
    public void ConstructorRejectsUnsupportedDimensions(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageSize(width, height));
    }

    [Fact]
    public void FitWithinPreservesAspectRatioWithoutUpscaling()
    {
        ImageSize source = new(10_000, 5_000);

        ImageSize fitted = source.FitWithin(1_024, 1_024);

        Assert.Equal(new ImageSize(1_024, 512), fitted);
    }

    [Fact]
    public void FitWithinReturnsOriginalWhenItAlreadyFits()
    {
        ImageSize source = new(800, 600);

        ImageSize fitted = source.FitWithin(1_024, 1_024);

        Assert.Equal(source, fitted);
    }
}
