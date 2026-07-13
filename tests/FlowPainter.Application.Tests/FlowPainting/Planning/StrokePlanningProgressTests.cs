using FlowPainter.Application.FlowPainting.Planning;

namespace FlowPainter.Application.Tests.FlowPainting.Planning;

public sealed class StrokePlanningProgressTests
{
    [Fact]
    public void ConstructorAcceptsCompletedProgress()
    {
        StrokePlanningProgress progress = new(StrokePlanningStage.Completed, 10, 10, 1d);

        Assert.Equal(StrokePlanningStage.Completed, progress.Stage);
        Assert.Equal(1d, progress.Fraction);
    }

    [Fact]
    public void ConstructorRejectsCompletedCountAboveTotal()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new StrokePlanningProgress(StrokePlanningStage.PlanningStrokes, 2, 1, 0.5d));
    }

    [Theory]
    [InlineData(-0.1d)]
    [InlineData(1.1d)]
    [InlineData(double.NaN)]
    public void ConstructorRejectsInvalidFraction(double fraction)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new StrokePlanningProgress(StrokePlanningStage.Preparing, 0, 1, fraction));
    }
}
