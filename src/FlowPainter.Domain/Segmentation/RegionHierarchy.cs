using System.Collections.ObjectModel;

namespace FlowPainter.Domain.Segmentation;

public sealed class RegionHierarchy
{
    public RegionHierarchy(int fineRegionCount, IEnumerable<RegionHierarchyLevel> levels)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fineRegionCount);
        ArgumentNullException.ThrowIfNull(levels);

        RegionHierarchyLevel[] levelArray = levels.OrderBy(level => level.Level).ToArray();
        if (levelArray.Length == 0)
        {
            throw new ArgumentException("The hierarchy must contain at least the identity level.", nameof(levels));
        }

        for (int levelIndex = 0; levelIndex < levelArray.Length; levelIndex++)
        {
            RegionHierarchyLevel level = levelArray[levelIndex];
            if (level.Level != levelIndex)
            {
                throw new ArgumentException("Hierarchy levels must be compact and start at zero.", nameof(levels));
            }

            if (level.FineRegionCount != fineRegionCount)
            {
                throw new ArgumentException("Every hierarchy level must map every fine region.", nameof(levels));
            }

            if (levelIndex == 0)
            {
                ValidateIdentityLevel(level, fineRegionCount, nameof(levels));
            }
            else
            {
                ValidateCoarsening(levelArray[levelIndex - 1], level, nameof(levels));
            }
        }

        FineRegionCount = fineRegionCount;
        Levels = Array.AsReadOnly(levelArray);
    }

    public int FineRegionCount { get; }

    public ReadOnlyCollection<RegionHierarchyLevel> Levels { get; }

    public static RegionHierarchy CreateIdentity(int fineRegionCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fineRegionCount);
        int[] identity = Enumerable.Range(0, fineRegionCount).ToArray();
        return new RegionHierarchy(
            fineRegionCount,
            new[] { new RegionHierarchyLevel(0, fineRegionCount, identity) });
    }

    private static void ValidateIdentityLevel(
        RegionHierarchyLevel level,
        int fineRegionCount,
        string parameterName)
    {
        if (level.ParentRegionCount != fineRegionCount)
        {
            throw new ArgumentException("Hierarchy level zero must preserve every fine region.", parameterName);
        }

        for (int regionId = 0; regionId < fineRegionCount; regionId++)
        {
            if (level.GetParentId(regionId) != regionId)
            {
                throw new ArgumentException("Hierarchy level zero must be an identity mapping.", parameterName);
            }
        }
    }

    private static void ValidateCoarsening(
        RegionHierarchyLevel previous,
        RegionHierarchyLevel current,
        string parameterName)
    {
        if (current.ParentRegionCount > previous.ParentRegionCount)
        {
            throw new ArgumentException("A hierarchy level cannot contain more regions than its predecessor.", parameterName);
        }

        int[] previousToCurrent = Enumerable.Repeat(-1, previous.ParentRegionCount).ToArray();
        for (int fineRegionId = 0; fineRegionId < previous.FineRegionCount; fineRegionId++)
        {
            int previousParent = previous.GetParentId(fineRegionId);
            int currentParent = current.GetParentId(fineRegionId);
            int mappedParent = previousToCurrent[previousParent];
            if (mappedParent == -1)
            {
                previousToCurrent[previousParent] = currentParent;
            }
            else if (mappedParent != currentParent)
            {
                throw new ArgumentException(
                    "A coarser hierarchy level cannot split a region from its predecessor.",
                    parameterName);
            }
        }
    }
}
