using FlowPainter.Domain.Images;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Domain.Tests.Segmentation;

public sealed class RegionLabelMapTests
{
    private static readonly int[] TwoPixelsPerRegion = { 2, 2, 2 };

    [Fact]
    public void CreateUsesUInt16ForCompactRegionCounts()
    {
        RegionLabelMap map = RegionLabelMap.Create(
            new ImageSize(2, 2),
            2,
            new uint[] { 0, 0, 1, 1 });

        Assert.Equal(RegionLabelStorageKind.Compact, map.StorageKind);
        Assert.Equal(8L, map.RequiredBytes);
    }

    [Fact]
    public void SelectStorageUsesUInt16AtMaximumCompactBoundary()
    {
        RegionLabelStorageKind kind = RegionLabelMap.SelectStorage(
            RegionLabelMap.MaximumUInt16RegionCount);

        Assert.Equal(RegionLabelStorageKind.Compact, kind);
    }

    [Fact]
    public void SelectStorageUsesUInt32AboveCompactBoundary()
    {
        RegionLabelStorageKind kind = RegionLabelMap.SelectStorage(
            RegionLabelMap.MaximumUInt16RegionCount + 1);

        Assert.Equal(RegionLabelStorageKind.Wide, kind);
    }

    [Fact]
    public void GetRequiredBytesUsesSelectedStorageWidth()
    {
        ImageSize size = new(300, 300);

        long compactBytes = RegionLabelMap.GetRequiredBytes(size, 100);
        long wideBytes = RegionLabelMap.GetRequiredBytes(
            size,
            RegionLabelMap.MaximumUInt16RegionCount + 1);

        Assert.Equal(180_000L, compactBytes);
        Assert.Equal(360_000L, wideBytes);
    }

    [Fact]
    public void CreateCopiesInputLabels()
    {
        uint[] labels = { 0, 0, 1, 1 };
        RegionLabelMap map = RegionLabelMap.Create(new ImageSize(2, 2), 2, labels);

        labels[0] = 1;

        Assert.Equal(0u, map[0, 0]);
    }

    [Fact]
    public void GetRowProvidesReadOnlyLabelAccess()
    {
        RegionLabelMap map = RegionLabelMap.Create(
            new ImageSize(3, 2),
            3,
            new uint[] { 0, 1, 1, 0, 2, 2 });

        RegionLabelRow row = map.GetRow(1);
        uint[] copy = new uint[3];
        row.CopyTo(copy);

        Assert.Equal(new uint[] { 0, 2, 2 }, copy);
    }

    [Fact]
    public void CreateRejectsIncorrectLabelCount()
    {
        Assert.Throws<ArgumentException>(() => RegionLabelMap.Create(
            new ImageSize(2, 2),
            2,
            new uint[] { 0, 1, 1 }));
    }

    [Fact]
    public void CreateRejectsLabelOutsideCompactRange()
    {
        Assert.Throws<ArgumentException>(() => RegionLabelMap.Create(
            new ImageSize(2, 2),
            2,
            new uint[] { 0, 0, 1, 2 }));
    }

    [Fact]
    public void CreateRejectsUnusedCompactLabel()
    {
        Assert.Throws<ArgumentException>(() => RegionLabelMap.Create(
            new ImageSize(2, 2),
            3,
            new uint[] { 0, 0, 2, 2 }));
    }

    [Fact]
    public void CreateRejectsMoreRegionsThanPixels()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RegionLabelMap.Create(
            new ImageSize(2, 2),
            5,
            new uint[] { 0, 1, 2, 3 }));
    }

    [Fact]
    public void IndexerRejectsCoordinatesOutsideImage()
    {
        RegionLabelMap map = RegionLabelMap.Create(
            new ImageSize(2, 2),
            1,
            new uint[] { 0, 0, 0, 0 });

        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = map[2, 0]; });
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = map[0, 2]; });
    }

    [Fact]
    public void CountPixelsByRegionMatchesLabels()
    {
        RegionLabelMap map = RegionLabelMap.Create(
            new ImageSize(3, 2),
            3,
            new uint[] { 0, 1, 1, 0, 2, 2 });

        Assert.Equal(TwoPixelsPerRegion, map.CountPixelsByRegion());
    }

    [Fact]
    public void CreateAcceptsSignedAssignmentBufferAndCopiesIt()
    {
        int[] labels = { 0, 0, 1, 1 };
        RegionLabelMap map = RegionLabelMap.Create(new ImageSize(2, 2), 2, labels);

        labels[0] = 1;

        Assert.Equal(RegionLabelStorageKind.Compact, map.StorageKind);
        Assert.Equal(0u, map[0, 0]);
    }

    [Fact]
    public void CreateRejectsNegativeSignedLabel()
    {
        Assert.Throws<ArgumentException>(() => RegionLabelMap.Create(
            new ImageSize(2, 2),
            2,
            new int[] { 0, -1, 1, 1 }));
    }
}
