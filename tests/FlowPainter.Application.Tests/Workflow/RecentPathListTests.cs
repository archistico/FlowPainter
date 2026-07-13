using FlowPainter.Application.Workflow;

namespace FlowPainter.Application.Tests.Workflow;

public sealed class RecentPathListTests
{
    [Fact]
    public void PathsExposeReadOnlyView()
    {
        RecentPathList list = new();
        list.Add(Path.Combine(Path.GetTempPath(), "project.flowpainter.json"));
        IList<string> paths = Assert.IsAssignableFrom<IList<string>>(list.Paths);

        Assert.Throws<NotSupportedException>(() => paths.Clear());
        Assert.Single(list.Paths);
    }

    [Fact]
    public void AddPlacesNewestPathFirst()
    {
        RecentPathList list = new();
        string first = Path.Combine(Path.GetTempPath(), "first.flowpainter.json");
        string second = Path.Combine(Path.GetTempPath(), "second.flowpainter.json");

        list.Add(first);
        list.Add(second);

        Assert.Equal(Path.GetFullPath(second), list.Paths[0]);
        Assert.Equal(Path.GetFullPath(first), list.Paths[1]);
    }

    [Fact]
    public void AddMovesExistingPathToFrontIgnoringCase()
    {
        RecentPathList list = new();
        string first = Path.Combine(Path.GetTempPath(), "Project.flowpainter.json");
        string second = Path.Combine(Path.GetTempPath(), "other.flowpainter.json");

        list.Add(first);
        list.Add(second);
        list.Add(first.ToUpperInvariant());

        Assert.Equal(2, list.Paths.Count);
        Assert.Equal(Path.GetFullPath(first.ToUpperInvariant()), list.Paths[0]);
    }

    [Fact]
    public void AddEnforcesCapacity()
    {
        RecentPathList list = new(2);

        list.Add(Path.Combine(Path.GetTempPath(), "one"));
        list.Add(Path.Combine(Path.GetTempPath(), "two"));
        list.Add(Path.Combine(Path.GetTempPath(), "three"));

        Assert.Equal(2, list.Paths.Count);
        Assert.DoesNotContain(list.Paths, path => path.EndsWith("one", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AddRejectsEmptyPath()
    {
        Assert.Throws<ArgumentException>(() => new RecentPathList().Add("  "));
    }

    [Fact]
    public void ConstructorRejectsInvalidCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecentPathList(0));
    }

    [Fact]
    public void RemoveDeletesNormalizedPath()
    {
        RecentPathList list = new();
        string path = Path.Combine(Path.GetTempPath(), "project.flowpainter.json");
        list.Add(path);

        Assert.True(list.Remove(path));
        Assert.Empty(list.Paths);
    }

    [Fact]
    public void RemoveReturnsFalseForEmptyOrMissingPath()
    {
        RecentPathList list = new();

        Assert.False(list.Remove(""));
        Assert.False(list.Remove(Path.Combine(Path.GetTempPath(), "missing")));
    }

    [Fact]
    public void ReplacePreservesInputOrderAndCapacity()
    {
        RecentPathList list = new(2);
        string first = Path.Combine(Path.GetTempPath(), "first");
        string second = Path.Combine(Path.GetTempPath(), "second");
        string third = Path.Combine(Path.GetTempPath(), "third");

        list.Replace([first, second, third]);

        Assert.Equal(Path.GetFullPath(first), list.Paths[0]);
        Assert.Equal(Path.GetFullPath(second), list.Paths[1]);
    }

    [Fact]
    public void ClearRemovesAllPaths()
    {
        RecentPathList list = new();
        list.Add(Path.Combine(Path.GetTempPath(), "one"));

        list.Clear();

        Assert.Empty(list.Paths);
    }
}
