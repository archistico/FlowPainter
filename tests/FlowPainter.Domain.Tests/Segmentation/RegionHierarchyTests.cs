using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Domain.Tests.Segmentation;

public sealed class RegionHierarchyTests
{
    [Fact]
    public void CreateIdentityMapsEveryFineRegionToItself()
    {
        RegionHierarchy hierarchy = RegionHierarchy.CreateIdentity(3);

        Assert.Single(hierarchy.Levels);
        Assert.Equal(0, hierarchy.Levels[0].GetParentId(0));
        Assert.Equal(1, hierarchy.Levels[0].GetParentId(1));
        Assert.Equal(2, hierarchy.Levels[0].GetParentId(2));
    }

    [Fact]
    public void ConstructorAcceptsDeterministicCoarsening()
    {
        RegionHierarchy hierarchy = new(
            4,
            new[]
            {
                new RegionHierarchyLevel(0, 4, new[] { 0, 1, 2, 3 }),
                new RegionHierarchyLevel(1, 2, new[] { 0, 0, 1, 1 }),
                new RegionHierarchyLevel(2, 1, new[] { 0, 0, 0, 0 }),
            });

        Assert.Equal(3, hierarchy.Levels.Count);
        Assert.Equal(1, hierarchy.Levels[2].ParentRegionCount);
    }

    [Fact]
    public void HierarchyLevelCopiesParentMapping()
    {
        int[] parents = { 0, 0, 1 };
        RegionHierarchyLevel level = new(1, 2, parents);

        parents[0] = 1;

        Assert.Equal(0, level.GetParentId(0));
    }

    [Fact]
    public void ConstructorRejectsMissingIdentityLevel()
    {
        Assert.Throws<ArgumentException>(() => new RegionHierarchy(
            2,
            new[] { new RegionHierarchyLevel(1, 1, new[] { 0, 0 }) }));
    }

    [Fact]
    public void ConstructorRejectsNonIdentityLevelZero()
    {
        Assert.Throws<ArgumentException>(() => new RegionHierarchy(
            2,
            new[] { new RegionHierarchyLevel(0, 1, new[] { 0, 0 }) }));
    }

    [Fact]
    public void ConstructorRejectsCoarserLevelThatSplitsPreviousParent()
    {
        Assert.Throws<ArgumentException>(() => new RegionHierarchy(
            4,
            new[]
            {
                new RegionHierarchyLevel(0, 4, new[] { 0, 1, 2, 3 }),
                new RegionHierarchyLevel(1, 2, new[] { 0, 0, 1, 1 }),
                new RegionHierarchyLevel(2, 2, new[] { 0, 1, 0, 1 }),
            }));
    }

    [Fact]
    public void HierarchyLevelRejectsUnusedCompactParent()
    {
        Assert.Throws<ArgumentException>(() => new RegionHierarchyLevel(
            1,
            3,
            new[] { 0, 0, 2, 2 }));
    }
}
