using FlowPainter.Application.Boundaries;

namespace FlowPainter.Application.Tests.Boundaries;

public sealed class SceneBoundaryAnalysisProgressTests
{
    [Fact]
    public void ConstructorAcceptsValidProgress()
    {
        SceneBoundaryAnalysisProgress progress = new(
            SceneBoundaryAnalysisStage.LinkingContours,
            5,
            10,
            0.5d);

        Assert.Equal(5, progress.CompletedRows);
        Assert.Equal(0.5d, progress.Fraction);
    }

    [Fact]
    public void ConstructorRejectsCompletedRowsAboveTotal()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SceneBoundaryAnalysisProgress(
            SceneBoundaryAnalysisStage.Completed,
            2,
            1,
            1d));
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(1.01d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidFraction(double fraction)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SceneBoundaryAnalysisProgress(
            SceneBoundaryAnalysisStage.Preparing,
            0,
            1,
            fraction));
    }
}
