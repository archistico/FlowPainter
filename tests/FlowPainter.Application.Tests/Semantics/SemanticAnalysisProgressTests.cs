using FlowPainter.Application.Semantics;

namespace FlowPainter.Application.Tests.Semantics;

public sealed class SemanticAnalysisProgressTests
{
    [Fact]
    public void ConstructorRetainsValues()
    {
        SemanticAnalysisProgress progress = new(
            SemanticAnalysisStage.SegmentingSubjects,
            5,
            12,
            0.6d);

        Assert.Equal(SemanticAnalysisStage.SegmentingSubjects, progress.Stage);
        Assert.Equal(5, progress.CompletedRows);
        Assert.Equal(12, progress.TotalRows);
        Assert.Equal(0.6d, progress.Fraction);
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(0, -1)]
    [InlineData(11, 10)]
    public void ConstructorRejectsInvalidRowCounts(int completed, int total)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SemanticAnalysisProgress(
                SemanticAnalysisStage.Preparing,
                completed,
                total,
                0d));
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(1.01d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidFraction(double fraction)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SemanticAnalysisProgress(
                SemanticAnalysisStage.Preparing,
                0,
                1,
                fraction));
    }

    [Fact]
    public void ConstructorRejectsUnknownStage()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SemanticAnalysisProgress(
                (SemanticAnalysisStage)999,
                0,
                1,
                0d));
    }
}
