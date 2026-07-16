using System.Collections.ObjectModel;

namespace FlowPainter.Domain.Segmentation;

public sealed class RegionAdjacencyGraph
{
    private readonly Dictionary<(int First, int Second), RegionAdjacency> _lookup;

    public RegionAdjacencyGraph(int regionCount, IEnumerable<RegionAdjacency> edges)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(regionCount);
        ArgumentNullException.ThrowIfNull(edges);

        RegionAdjacency[] orderedEdges = edges
            .OrderBy(edge => edge.FirstRegionId)
            .ThenBy(edge => edge.SecondRegionId)
            .ToArray();

        _lookup = new Dictionary<(int First, int Second), RegionAdjacency>(orderedEdges.Length);
        foreach (RegionAdjacency edge in orderedEdges)
        {
            if (edge.SecondRegionId >= regionCount)
            {
                throw new ArgumentException("An adjacency references a region outside the graph.", nameof(edges));
            }

            if (!_lookup.TryAdd((edge.FirstRegionId, edge.SecondRegionId), edge))
            {
                throw new ArgumentException("Duplicate adjacency edges are not allowed.", nameof(edges));
            }
        }

        RegionCount = regionCount;
        Edges = Array.AsReadOnly(orderedEdges);
    }

    public int RegionCount { get; }

    public ReadOnlyCollection<RegionAdjacency> Edges { get; }

    public bool TryGetEdge(int firstRegionId, int secondRegionId, out RegionAdjacency? edge)
    {
        ValidateRegionId(firstRegionId);
        ValidateRegionId(secondRegionId);

        if (firstRegionId == secondRegionId)
        {
            edge = null;
            return false;
        }

        (int First, int Second) key = firstRegionId < secondRegionId
            ? (firstRegionId, secondRegionId)
            : (secondRegionId, firstRegionId);
        return _lookup.TryGetValue(key, out edge);
    }

    public static RegionAdjacencyGraph CreateEmpty(int regionCount)
    {
        return new RegionAdjacencyGraph(regionCount, Array.Empty<RegionAdjacency>());
    }

    private void ValidateRegionId(int regionId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(regionId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(regionId, RegionCount);
    }
}
