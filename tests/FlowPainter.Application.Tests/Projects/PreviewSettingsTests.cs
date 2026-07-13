using FlowPainter.Application.Projects;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Tests.Projects;

public sealed class PreviewSettingsTests
{
    [Theory]
    [InlineData(PreviewQuality.Draft, PreviewSettings.DraftMaximumDimension)]
    [InlineData(PreviewQuality.Standard, PreviewSettings.StandardMaximumDimension)]
    [InlineData(PreviewQuality.High, PreviewSettings.HighMaximumDimension)]
    public void MaximumDimensionMatchesQuality(PreviewQuality quality, int expected)
    {
        PreviewSettings settings = new(quality);

        Assert.Equal(expected, settings.MaximumDimension);
    }

    [Fact]
    public void ConstructorRejectsUnknownQuality()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PreviewSettings((PreviewQuality)99));
    }

    [Fact]
    public void FitPreservesAspectRatio()
    {
        PreviewSettings settings = new(PreviewQuality.Draft);

        ImageSize fitted = settings.Fit(new ImageSize(1000, 500));

        Assert.Equal(new ImageSize(256, 128), fitted);
    }

    [Fact]
    public void FitDoesNotUpscaleSmallImage()
    {
        PreviewSettings settings = new(PreviewQuality.High);
        ImageSize source = new(320, 200);

        Assert.Equal(source, settings.Fit(source));
    }
}
