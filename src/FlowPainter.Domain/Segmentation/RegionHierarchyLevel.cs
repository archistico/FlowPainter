using System.Collections.ObjectModel;

namespace FlowPainter.Domain.Segmentation;

public sealed class RegionHierarchyLevel
{
    private readonly ReadOnlyCollection<int>[] _fineRegionsByParent;
    private readonly int[] _fineRegionParents;

    public RegionHierarchyLevel(int level, int parentRegionCount, ReadOnlySpan<int> fineRegionParents)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(level);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(parentRegionCount);
        if (fineRegionParents.IsEmpty)
        {
            throw new ArgumentException("At least one fine-region parent is required.", nameof(fineRegionParents));
        }

        bool[] usedParents = new bool[parentRegionCount];
        List<int>[] mutableFineRegionsByParent = new List<int>[parentRegionCount];
        for (int parentId = 0; parentId < parentRegionCount; parentId++)
        {
            mutableFineRegionsByParent[parentId] = new List<int>();
        }

        _fineRegionParents = fineRegionParents.ToArray();
        for (int fineRegionId = 0; fineRegionId < _fineRegionParents.Length; fineRegionId++)
        {
            int parentId = _fineRegionParents[fineRegionId];
            if (parentId < 0 || parentId >= parentRegionCount)
            {
                throw new ArgumentException(
                    $"Parent identifier {parentId} is outside the compact range.",
                    nameof(fineRegionParents));
            }

            usedParents[parentId] = true;
            mutableFineRegionsByParent[parentId].Add(fineRegionId);
        }

        for (int parentId = 0; parentId < usedParents.Length; parentId++)
        {
            if (!usedParents[parentId])
            {
                throw new ArgumentException(
                    $"Compact parent identifier {parentId} is unused.",
                    nameof(fineRegionParents));
            }
        }

        _fineRegionsByParent = new ReadOnlyCollection<int>[parentRegionCount];
        for (int parentId = 0; parentId < parentRegionCount; parentId++)
        {
            _fineRegionsByParent[parentId] = mutableFineRegionsByParent[parentId].AsReadOnly();
        }

        Level = level;
        ParentRegionCount = parentRegionCount;
    }

    public int Level { get; }

    public int FineRegionCount => _fineRegionParents.Length;

    public int ParentRegionCount { get; }

    public int GetParentId(int fineRegionId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fineRegionId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(fineRegionId, FineRegionCount);
        return _fineRegionParents[fineRegionId];
    }

    public ReadOnlyCollection<int> GetFineRegionIds(int parentRegionId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(parentRegionId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(parentRegionId, ParentRegionCount);
        return _fineRegionsByParent[parentRegionId];
    }

    public int[] CopyParentIds()
    {
        return (int[])_fineRegionParents.Clone();
    }
}
