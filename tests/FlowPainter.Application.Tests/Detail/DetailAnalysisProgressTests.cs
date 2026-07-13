using FlowPainter.Application.Detail;

namespace FlowPainter.Application.Tests.Detail;

public sealed class DetailAnalysisProgressTests
{
    [Fact]
    public void ConstructorRetainsValidValues()
    {
        DetailAnalysisProgress progress = new(
            DetailAnalysisStage.AnalyzingStructure,
            3,
            10,
            0.25d);

        Assert.Equal(DetailAnalysisStage.AnalyzingStructure, progress.Stage);
        Assert.Equal(3, progress.CompletedRows);
        Assert.Equal(10, progress.TotalRows);
        Assert.Equal(0.25d, progress.Fraction);
    }

    [Fact]
    public void ConstructorRejectsCompletedRowsAboveTotal()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DetailAnalysisProgress(DetailAnalysisStage.Completed, 2, 1, 1d));
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(1.01d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidFraction(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DetailAnalysisProgress(DetailAnalysisStage.Preparing, 0, 1, value));
    }
}
