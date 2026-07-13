namespace FlowPainter.Application.Workflow;

public sealed class RecentPathList
{
    public const int DefaultCapacity = 10;

    private readonly List<string> _paths = [];
    private readonly IReadOnlyList<string> _readOnlyPaths;

    public RecentPathList(int capacity = DefaultCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        Capacity = capacity;
        _readOnlyPaths = _paths.AsReadOnly();
    }

    public int Capacity { get; }

    public IReadOnlyList<string> Paths => _readOnlyPaths;

    public void Add(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A recent path cannot be empty.", nameof(path));
        }

        string normalized = Path.GetFullPath(path.Trim());
        _paths.RemoveAll(item => string.Equals(item, normalized, StringComparison.OrdinalIgnoreCase));
        _paths.Insert(0, normalized);

        if (_paths.Count > Capacity)
        {
            _paths.RemoveRange(Capacity, _paths.Count - Capacity);
        }
    }

    public bool Remove(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        string normalized = Path.GetFullPath(path.Trim());
        return _paths.RemoveAll(item => string.Equals(item, normalized, StringComparison.OrdinalIgnoreCase)) > 0;
    }

    public void Clear()
    {
        _paths.Clear();
    }

    public void Replace(IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        _paths.Clear();
        foreach (string path in paths.Reverse())
        {
            Add(path);
        }
    }
}
