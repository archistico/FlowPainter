using FlowPainter.Application.Detail;

namespace FlowPainter.Application.Tests.Detail;

public sealed class DetailAnalysisSettingsTests
{
    [Fact]
    public void ConstructorUsesStructuralDefaults()
    {
        DetailAnalysisSettings settings = new();

        Assert.Equal(DetailAnalysisSettings.DefaultBaseDetail, settings.BaseDetail);
        Assert.Equal(DetailAnalysisSettings.DefaultEdgeWeight, settings.EdgeWeight);
        Assert.Equal(DetailAnalysisSettings.DefaultContrastWeight, settings.ContrastWeight);
        Assert.Equal(DetailAnalysisSettings.DefaultSmoothingRadius, settings.SmoothingRadius);
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(1.01d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidBaseDetail(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DetailAnalysisSettings(baseDetail: value));
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(4.01d)]
    [InlineData(double.PositiveInfinity)]
    public void ConstructorRejectsInvalidWeights(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DetailAnalysisSettings(edgeWeight: value));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(17)]
    public void ConstructorRejectsInvalidSmoothingRadius(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DetailAnalysisSettings(smoothingRadius: value));
    }

    [Fact]
    public void ConstructorRejectsAllZeroStructuralWeights()
    {
        Assert.Throws<ArgumentException>(
            () => new DetailAnalysisSettings(edgeWeight: 0d, contrastWeight: 0d));
    }
}
