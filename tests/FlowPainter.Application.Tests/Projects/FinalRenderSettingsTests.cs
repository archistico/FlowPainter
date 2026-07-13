using FlowPainter.Application.Projects;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.Projects;

public sealed class FinalRenderSettingsTests
{
    [Fact]
    public void ConstructorUsesProductionDefaults()
    {
        FinalRenderSettings settings = new();

        Assert.Equal(FinalRenderSettings.DefaultMaximumDimension, settings.MaximumDimension);
        Assert.Equal(RasterImageFormat.Png, settings.Format);
        Assert.Equal(FinalRenderSettings.DefaultJpegQuality, settings.JpegQuality);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4096)]
    [InlineData(10_000)]
    public void ConstructorAcceptsSupportedMaximumDimension(int maximumDimension)
    {
        FinalRenderSettings settings = new(maximumDimension);

        Assert.Equal(maximumDimension, settings.MaximumDimension);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10_001)]
    public void ConstructorRejectsUnsupportedMaximumDimension(int maximumDimension)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FinalRenderSettings(maximumDimension));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(92)]
    [InlineData(100)]
    public void ConstructorAcceptsSupportedJpegQuality(int quality)
    {
        FinalRenderSettings settings = new(jpegQuality: quality);

        Assert.Equal(quality, settings.JpegQuality);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void ConstructorRejectsUnsupportedJpegQuality(int quality)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FinalRenderSettings(jpegQuality: quality));
    }

    [Fact]
    public void ConstructorRejectsUnknownFormat()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FinalRenderSettings(format: (RasterImageFormat)99));
    }

    [Fact]
    public void GetOutputSizePreservesLandscapeAspectRatioAndUpscales()
    {
        FinalRenderSettings settings = new(4000);

        ImageSize output = settings.GetOutputSize(new ImageSize(1000, 500));

        Assert.Equal(new ImageSize(4000, 2000), output);
    }

    [Fact]
    public void GetOutputSizePreservesPortraitAspectRatio()
    {
        FinalRenderSettings settings = new(3000);

        ImageSize output = settings.GetOutputSize(new ImageSize(1000, 2000));

        Assert.Equal(new ImageSize(1500, 3000), output);
    }

    [Fact]
    public void GetOutputSizeCanDownscaleLargeSource()
    {
        FinalRenderSettings settings = new(2000);

        ImageSize output = settings.GetOutputSize(new ImageSize(8000, 4000));

        Assert.Equal(new ImageSize(2000, 1000), output);
    }

    [Theory]
    [InlineData(RasterImageFormat.Png, "png")]
    [InlineData(RasterImageFormat.Jpeg, "jpg")]
    public void DefaultFileExtensionMatchesFormat(RasterImageFormat format, string extension)
    {
        FinalRenderSettings settings = new(format: format);

        Assert.Equal(extension, settings.DefaultFileExtension);
    }
}
