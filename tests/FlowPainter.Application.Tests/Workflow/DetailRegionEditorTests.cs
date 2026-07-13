using FlowPainter.Application.Workflow;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;

namespace FlowPainter.Application.Tests.Workflow;

public sealed class DetailRegionEditorTests
{
    private static readonly string[] MoveEarlierExpectedOrder =
    [
        "manual-0001",
        "manual-0003",
        "manual-0002"
    ];

    private static readonly string[] MoveLaterExpectedOrder =
    [
        "manual-0002",
        "manual-0001",
        "manual-0003"
    ];

    [Fact]
    public void AddCreatesSequentialIdentifierAndDefaultLabel()
    {
        DetailRegionEditor editor = new();

        DetailRegion first = editor.Add(CreateBounds(), 0.8d, DetailRegionIntent.IncreaseDetail);
        DetailRegion second = editor.Add(CreateBounds(0.5d), 0.6d, DetailRegionIntent.ReduceDetail);

        Assert.Equal("manual-0001", first.Id);
        Assert.Equal("Focus 1", first.Label);
        Assert.Equal("manual-0002", second.Id);
        Assert.Equal("Background 2", second.Label);
    }

    [Fact]
    public void AddRejectsRegionBelowMinimumSize()
    {
        DetailRegionEditor editor = new();
        NormalizedRect tooSmall = new(0.1d, 0.1d, 0.101d, 0.3d);

        Assert.Throws<ArgumentOutOfRangeException>(() => editor.Add(
            tooSmall,
            0.8d,
            DetailRegionIntent.IncreaseDetail));
        Assert.Empty(editor.Regions);
    }

    [Fact]
    public void AddUsesTrimmedCustomLabel()
    {
        DetailRegionEditor editor = new();

        DetailRegion region = editor.Add(CreateBounds(), 0.8d, DetailRegionIntent.IncreaseDetail, "  Eyes  ");

        Assert.Equal("Eyes", region.Label);
    }

    [Fact]
    public void RegionsExposeReadOnlyView()
    {
        DetailRegionEditor editor = new();
        editor.Add(CreateBounds(), 0.8d, DetailRegionIntent.IncreaseDetail);
        IList<DetailRegion> regions = Assert.IsAssignableFrom<IList<DetailRegion>>(editor.Regions);

        Assert.Throws<NotSupportedException>(() => regions.Clear());
        Assert.Single(editor.Regions);
    }

    [Fact]
    public void ReplaceAllCopiesRegionsAndAdvancesIdentifier()
    {
        DetailRegionEditor editor = new();
        DetailRegion[] regions = [CreateRegion("manual-0012")];

        editor.ReplaceAll(regions);
        DetailRegion added = editor.Add(CreateBounds(0.5d), 0.5d, DetailRegionIntent.IncreaseDetail);

        Assert.Equal("manual-0013", added.Id);
    }

    [Fact]
    public void ReplaceAllRejectsSmallRegionsWithoutChangingCurrentState()
    {
        DetailRegionEditor editor = new();
        editor.Add(CreateBounds(), 0.8d, DetailRegionIntent.IncreaseDetail);
        DetailRegion invalid = new(
            "manual-0010",
            new NormalizedRect(0.1d, 0.1d, 0.101d, 0.3d),
            0.8d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.IncreaseDetail);

        Assert.Throws<ArgumentOutOfRangeException>(() => editor.ReplaceAll([invalid]));
        Assert.Single(editor.Regions);
        Assert.Equal("manual-0001", editor.Regions[0].Id);
    }

    [Fact]
    public void ReplaceAllRejectsDuplicateIdentifiersIgnoringCase()
    {
        DetailRegionEditor editor = new();
        DetailRegion[] regions = [CreateRegion("manual-0001"), CreateRegion("MANUAL-0001")];

        Assert.Throws<ArgumentException>(() => editor.ReplaceAll(regions));
    }

    [Fact]
    public void UpdatePreservesIdentifierAndOrigin()
    {
        DetailRegionEditor editor = new();
        DetailRegion original = editor.Add(CreateBounds(), 0.8d, DetailRegionIntent.IncreaseDetail);
        NormalizedRect bounds = new(0.2d, 0.3d, 0.7d, 0.8d);

        DetailRegion updated = editor.Update(original.Id, bounds, 0.4d, DetailRegionIntent.ReduceDetail, "Background");

        Assert.Equal(original.Id, updated.Id);
        Assert.Equal(original.Origin, updated.Origin);
        Assert.Equal(bounds, updated.Bounds);
        Assert.Equal("Background", updated.Label);
    }

    [Fact]
    public void UpdateRejectsRegionBelowMinimumSize()
    {
        DetailRegionEditor editor = new();
        DetailRegion original = editor.Add(CreateBounds(), 0.8d, DetailRegionIntent.IncreaseDetail);
        NormalizedRect tooSmall = new(0.1d, 0.1d, 0.101d, 0.3d);

        Assert.Throws<ArgumentOutOfRangeException>(() => editor.Update(
            original.Id,
            tooSmall,
            original.Strength,
            original.Intent,
            original.Label));
        Assert.Equal(CreateBounds(), editor.Get(original.Id).Bounds);
    }

    [Fact]
    public void MoveClampsRegionToImageBounds()
    {
        DetailRegionEditor editor = new();
        DetailRegion original = editor.Add(new NormalizedRect(0.2d, 0.2d, 0.5d, 0.6d), 0.8d, DetailRegionIntent.IncreaseDetail);

        DetailRegion moved = editor.Move(original.Id, 2d, -2d);

        Assert.Equal(0.7d, moved.Bounds.Left, 12);
        Assert.Equal(0d, moved.Bounds.Top, 12);
        Assert.Equal(1d, moved.Bounds.Right, 12);
        Assert.Equal(0.4d, moved.Bounds.Bottom, 12);
    }

    [Theory]
    [InlineData(double.NaN, 0d)]
    [InlineData(0d, double.PositiveInfinity)]
    public void MoveRejectsNonFiniteDelta(double deltaX, double deltaY)
    {
        DetailRegionEditor editor = new();
        DetailRegion region = editor.Add(CreateBounds(), 0.8d, DetailRegionIntent.IncreaseDetail);

        Assert.Throws<ArgumentOutOfRangeException>(() => editor.Move(region.Id, deltaX, deltaY));
    }

    [Fact]
    public void ResizeUpdatesNormalizedBounds()
    {
        DetailRegionEditor editor = new();
        DetailRegion region = editor.Add(CreateBounds(), 0.8d, DetailRegionIntent.IncreaseDetail);

        DetailRegion resized = editor.Resize(region.Id, 0.1d, 0.15d, 0.4d, 0.5d);

        Assert.Equal(0.1d, resized.Bounds.Left, 12);
        Assert.Equal(0.15d, resized.Bounds.Top, 12);
        Assert.Equal(0.5d, resized.Bounds.Right, 12);
        Assert.Equal(0.65d, resized.Bounds.Bottom, 12);
    }

    [Theory]
    [InlineData(0.9d, 0.1d, 0.2d, 0.3d)]
    [InlineData(0.1d, 0.9d, 0.3d, 0.2d)]
    [InlineData(0.1d, 0.1d, 0.001d, 0.2d)]
    public void ResizeRejectsInvalidBounds(double left, double top, double width, double height)
    {
        DetailRegionEditor editor = new();
        DetailRegion region = editor.Add(CreateBounds(), 0.8d, DetailRegionIntent.IncreaseDetail);

        Assert.Throws<ArgumentOutOfRangeException>(() => editor.Resize(region.Id, left, top, width, height));
    }

    [Fact]
    public void MoveEarlierReordersRegions()
    {
        DetailRegionEditor editor = CreateThreeRegionEditor();

        bool moved = editor.MoveEarlier("manual-0003");

        Assert.True(moved);
        Assert.Equal(MoveEarlierExpectedOrder, editor.Regions.Select(region => region.Id));
    }

    [Fact]
    public void MoveEarlierReturnsFalseForFirstRegion()
    {
        DetailRegionEditor editor = CreateThreeRegionEditor();

        Assert.False(editor.MoveEarlier("manual-0001"));
    }

    [Fact]
    public void MoveLaterReordersRegions()
    {
        DetailRegionEditor editor = CreateThreeRegionEditor();

        bool moved = editor.MoveLater("manual-0001");

        Assert.True(moved);
        Assert.Equal(MoveLaterExpectedOrder, editor.Regions.Select(region => region.Id));
    }

    [Fact]
    public void MoveLaterReturnsFalseForLastRegion()
    {
        DetailRegionEditor editor = CreateThreeRegionEditor();

        Assert.False(editor.MoveLater("manual-0003"));
    }

    [Fact]
    public void RemoveDeletesMatchingRegionIgnoringCase()
    {
        DetailRegionEditor editor = CreateThreeRegionEditor();

        bool removed = editor.Remove("MANUAL-0002");

        Assert.True(removed);
        Assert.Equal(2, editor.Count);
    }

    [Fact]
    public void RemoveReturnsFalseForMissingRegion()
    {
        DetailRegionEditor editor = new();

        Assert.False(editor.Remove("missing"));
    }

    [Fact]
    public void RemoveLastReturnsRemovedRegion()
    {
        DetailRegionEditor editor = CreateThreeRegionEditor();

        DetailRegion? removed = editor.RemoveLast();

        Assert.NotNull(removed);
        Assert.Equal("manual-0003", removed.Id);
        Assert.Equal(2, editor.Count);
    }

    [Fact]
    public void RemoveLastReturnsNullWhenEmpty()
    {
        Assert.Null(new DetailRegionEditor().RemoveLast());
    }

    [Fact]
    public void GetThrowsForMissingRegion()
    {
        Assert.Throws<KeyNotFoundException>(() => new DetailRegionEditor().Get("missing"));
    }

    private static DetailRegionEditor CreateThreeRegionEditor()
    {
        DetailRegionEditor editor = new();
        editor.Add(CreateBounds(), 0.8d, DetailRegionIntent.IncreaseDetail);
        editor.Add(CreateBounds(0.3d), 0.7d, DetailRegionIntent.IncreaseDetail);
        editor.Add(CreateBounds(0.6d), 0.6d, DetailRegionIntent.ReduceDetail);
        return editor;
    }

    private static NormalizedRect CreateBounds(double offset = 0.1d)
    {
        return new NormalizedRect(offset, offset, offset + 0.2d, offset + 0.2d);
    }

    private static DetailRegion CreateRegion(string id)
    {
        return new DetailRegion(
            id,
            CreateBounds(),
            0.8d,
            DetailRegionOrigin.Manual,
            DetailRegionIntent.IncreaseDetail,
            "Region");
    }
}
