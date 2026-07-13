using FlowPainter.Application.Semantics;

namespace FlowPainter.Application.Tests.Semantics;

public sealed class SemanticAnalysisSettingsTests
{
    [Fact]
    public void ConstructorUsesSemanticDefaults()
    {
        SemanticAnalysisSettings settings = new();

        Assert.True(settings.Enabled);
        Assert.Equal(SemanticAnalysisSettings.DefaultOverallInfluence, settings.OverallInfluence);
        Assert.Equal(SemanticAnalysisSettings.DefaultSaliencyWeight, settings.SaliencyWeight);
        Assert.Equal(SemanticAnalysisSettings.DefaultSubjectWeight, settings.SubjectWeight);
        Assert.Equal(SemanticAnalysisSettings.DefaultSilhouetteWeight, settings.SilhouetteWeight);
        Assert.Equal(SemanticAnalysisSettings.DefaultFocalWeight, settings.FocalWeight);
        Assert.Equal(SemanticAnalysisSettings.DefaultSubjectThreshold, settings.SubjectThreshold);
        Assert.Equal(SemanticAnalysisSettings.DefaultMinimumSubjectAreaRatio, settings.MinimumSubjectAreaRatio);
        Assert.Equal(SemanticAnalysisSettings.DefaultMaximumSubjects, settings.MaximumSubjects);
        Assert.Equal(SemanticAnalysisSettings.DefaultCenterBias, settings.CenterBias);
        Assert.Equal(SemanticAnalysisSettings.DefaultSmoothingRadius, settings.SmoothingRadius);
        Assert.Equal(SemanticAnalysisSettings.DefaultBoundaryRadius, settings.BoundaryRadius);
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(1.01d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidUnitIntervalValues(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SemanticAnalysisSettings(overallInfluence: value));
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(4.01d)]
    [InlineData(double.PositiveInfinity)]
    public void ConstructorRejectsInvalidWeights(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SemanticAnalysisSettings(subjectWeight: value));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(0.251d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidMinimumSubjectArea(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SemanticAnalysisSettings(minimumSubjectAreaRatio: value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(33)]
    public void ConstructorRejectsInvalidMaximumSubjects(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SemanticAnalysisSettings(maximumSubjects: value));
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(2.01d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidCenterBias(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SemanticAnalysisSettings(centerBias: value));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(17)]
    public void ConstructorRejectsInvalidRadii(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SemanticAnalysisSettings(smoothingRadius: value));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SemanticAnalysisSettings(boundaryRadius: value));
    }

    [Fact]
    public void ConstructorRejectsEnabledAnalysisWithAllMapsDisabled()
    {
        Assert.Throws<ArgumentException>(
            () => new SemanticAnalysisSettings(
                saliencyWeight: 0d,
                subjectWeight: 0d,
                silhouetteWeight: 0d,
                focalWeight: 0d));
    }

    [Fact]
    public void ConstructorAllowsDisabledAnalysisWithAllMapsDisabled()
    {
        SemanticAnalysisSettings settings = new(
            enabled: false,
            saliencyWeight: 0d,
            subjectWeight: 0d,
            silhouetteWeight: 0d,
            focalWeight: 0d);

        Assert.False(settings.Enabled);
    }
}
