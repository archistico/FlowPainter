using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Domain.Tests.Segmentation;

public sealed class RegionAdjacencyGraphTests
{
    [Fact]
    public void ConstructorSortsEdgesDeterministically()
    {
        RegionAdjacencyGraph graph = new(
            3,
            new[]
            {
                new RegionAdjacency(1, 2, 2),
                new RegionAdjacency(0, 2, 1),
                new RegionAdjacency(0, 1, 3),
            });

        Assert.Collection(
            graph.Edges,
            edge => Assert.True(edge.Connects(0, 1)),
            edge => Assert.True(edge.Connects(0, 2)),
            edge => Assert.True(edge.Connects(1, 2)));
    }

    [Fact]
    public void TryGetEdgeIsSymmetric()
    {
        RegionAdjacency expected = new(0, 1, 3, boundaryStrength: 0.8d);
        RegionAdjacencyGraph graph = new(2, new[] { expected });

        Assert.True(graph.TryGetEdge(0, 1, out RegionAdjacency? forward));
        Assert.True(graph.TryGetEdge(1, 0, out RegionAdjacency? reverse));
        Assert.Same(expected, forward);
        Assert.Same(expected, reverse);
    }

    [Fact]
    public void TryGetEdgeReturnsFalseForSelfLookup()
    {
        RegionAdjacencyGraph graph = RegionAdjacencyGraph.CreateEmpty(2);

        Assert.False(graph.TryGetEdge(1, 1, out RegionAdjacency? edge));
        Assert.Null(edge);
    }

    [Fact]
    public void ConstructorRejectsDuplicateEdges()
    {
        RegionAdjacency edge = new(0, 1, 2);

        Assert.Throws<ArgumentException>(() => new RegionAdjacencyGraph(2, new[] { edge, edge }));
    }

    [Fact]
    public void ConstructorRejectsEdgesOutsideGraph()
    {
        Assert.Throws<ArgumentException>(() => new RegionAdjacencyGraph(
            2,
            new[] { new RegionAdjacency(0, 2, 1) }));
    }

    [Fact]
    public void AdjacencyRejectsUnorderedOrInvalidValues()
    {
        Assert.Throws<ArgumentException>(() => new RegionAdjacency(1, 0, 1));
        Assert.Throws<ArgumentException>(() => new RegionAdjacency(0, 1, 1, meanGradient: 2d, maximumGradient: 1d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegionAdjacency(0, 1, 1, boundaryStrength: 1.1d));
    }
}
