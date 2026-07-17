using FlowPainter.Application.Segmentation;

namespace FlowPainter.Application.Tests.Segmentation;

public sealed class SegmentationDiagnosticsTests
{
    [Fact]
    public void ConstructorPreservesDiagnostics()
    {
        RegionSizeDistribution regionSizes = new(8, 20, 12d, 3d);
        SegmentationDiagnostics diagnostics = new(8, true, 0.2d, 10, 9, 2, 1, regionSizes);

        Assert.True(diagnostics.Converged);
        Assert.Equal(2, diagnostics.DisconnectedComponentsRepaired);
        Assert.Equal(1, diagnostics.UndersizedComponentsMerged);
        Assert.Same(regionSizes, diagnostics.RegionSizes);
    }

    [Fact]
    public void ConstructorRejectsInvalidCounts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SegmentationDiagnostics(0, false, 0d, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SegmentationDiagnostics(0, false, 0d, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SegmentationDiagnostics(0, false, 0d, 1, 2));
    }

    [Fact]
    public void ConstructorRejectsInvalidDisplacement()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SegmentationDiagnostics(0, false, -0.1d, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SegmentationDiagnostics(0, false, double.NaN, 1, 1));
    }
}
