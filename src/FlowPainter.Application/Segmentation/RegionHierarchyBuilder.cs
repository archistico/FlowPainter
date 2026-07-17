using FlowPainter.Application.Workloads;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Segmentation;

public static class RegionHierarchyBuilder
{
    private const long EstimatedWorkingBytesPerRegion = 512L;
    private const long EstimatedWorkingBytesPerEdge = 384L;

    public static RegionHierarchy Build(
        IReadOnlyList<ImageRegion> regions,
        RegionAdjacencyGraph adjacency,
        RegionMergeSettings settings,
        CancellationToken cancellationToken = default)
    {
        ValidateInputs(regions, adjacency, settings);
        EnsureMemoryWithinBudget(regions.Count, adjacency.Edges.Count);
        cancellationToken.ThrowIfCancellationRequested();

        int fineRegionCount = regions.Count;
        int[] identity = Enumerable.Range(0, fineRegionCount).ToArray();
        int intermediateTargetCount = CalculateTargetCount(
            fineRegionCount,
            fineRegionCount,
            settings.IntermediateTargetRatio);
        int[] intermediateParents = MergeLevel(
            regions,
            adjacency,
            identity,
            fineRegionCount,
            intermediateTargetCount,
            settings.IntermediateMaximumCost,
            settings,
            cancellationToken);
        int intermediateRegionCount = CountCompactParents(intermediateParents);

        int broadMassTargetCount = CalculateTargetCount(
            fineRegionCount,
            intermediateRegionCount,
            settings.BroadMassTargetRatio);
        int[] broadMassParents = MergeLevel(
            regions,
            adjacency,
            intermediateParents,
            intermediateRegionCount,
            broadMassTargetCount,
            settings.BroadMassMaximumCost,
            settings,
            cancellationToken);
        int broadMassRegionCount = CountCompactParents(broadMassParents);

        return new RegionHierarchy(
            fineRegionCount,
            new[]
            {
                new RegionHierarchyLevel(0, fineRegionCount, identity),
                new RegionHierarchyLevel(1, intermediateRegionCount, intermediateParents),
                new RegionHierarchyLevel(2, broadMassRegionCount, broadMassParents),
            });
    }

    private static int[] MergeLevel(
        IReadOnlyList<ImageRegion> fineRegions,
        RegionAdjacencyGraph fineAdjacency,
        ReadOnlySpan<int> startingFineParents,
        int startingGroupCount,
        int targetGroupCount,
        double maximumCost,
        RegionMergeSettings settings,
        CancellationToken cancellationToken)
    {
        if (startingGroupCount <= targetGroupCount)
        {
            return startingFineParents.ToArray();
        }

        int[] unionParents = Enumerable.Range(0, startingGroupCount).ToArray();
        MutableRegionGroup[] groups = CreateGroups(
            fineRegions,
            fineAdjacency,
            startingFineParents,
            startingGroupCount);
        Dictionary<(int First, int Second), MutableBoundary> boundaries = CreateBoundaries(
            fineAdjacency,
            startingFineParents,
            groups);
        PriorityQueue<MergeCandidate, MergePriority> candidates = new(
            MergePriorityComparer.Instance);
        foreach (MutableBoundary boundary in boundaries.Values)
        {
            EnqueueCandidate(candidates, groups, boundary, settings);
        }

        int activeGroupCount = startingGroupCount;
        int processedCandidateCount = 0;
        while (activeGroupCount > targetGroupCount
            && candidates.TryDequeue(out MergeCandidate candidate, out MergePriority priority))
        {
            if ((processedCandidateCount & 31) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            processedCandidateCount++;

            MutableRegionGroup first = groups[candidate.FirstRegionId];
            MutableRegionGroup second = groups[candidate.SecondRegionId];
            if (!first.IsActive
                || !second.IsActive
                || first.Version != candidate.FirstVersion
                || second.Version != candidate.SecondVersion
                || !boundaries.TryGetValue(
                    CreateKey(candidate.FirstRegionId, candidate.SecondRegionId),
                    out MutableBoundary? sharedBoundary))
            {
                continue;
            }

            double currentCost = CalculateCost(first, second, sharedBoundary, settings);
            if (Math.Abs(currentCost - priority.Cost) > 1e-12d)
            {
                EnqueueCandidate(candidates, groups, sharedBoundary, settings);
                continue;
            }

            if (currentCost > maximumCost)
            {
                break;
            }

            MergeGroups(
                first,
                second,
                sharedBoundary,
                groups,
                boundaries,
                candidates,
                unionParents,
                settings);
            activeGroupCount--;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return CreateCompactFineParentMapping(startingFineParents, unionParents);
    }

    private static MutableRegionGroup[] CreateGroups(
        IReadOnlyList<ImageRegion> fineRegions,
        RegionAdjacencyGraph fineAdjacency,
        ReadOnlySpan<int> startingFineParents,
        int startingGroupCount)
    {
        int totalPixelCount = 0;
        for (int regionId = 0; regionId < fineRegions.Count; regionId++)
        {
            totalPixelCount = checked(totalPixelCount + fineRegions[regionId].PixelCount);
        }

        MutableRegionGroup[] groups = new MutableRegionGroup[startingGroupCount];
        for (int groupId = 0; groupId < groups.Length; groupId++)
        {
            groups[groupId] = new MutableRegionGroup(groupId, totalPixelCount);
        }

        for (int fineRegionId = 0; fineRegionId < fineRegions.Count; fineRegionId++)
        {
            groups[startingFineParents[fineRegionId]].Add(fineRegions[fineRegionId]);
        }

        foreach (RegionAdjacency edge in fineAdjacency.Edges)
        {
            int firstGroupId = startingFineParents[edge.FirstRegionId];
            int secondGroupId = startingFineParents[edge.SecondRegionId];
            if (firstGroupId == secondGroupId)
            {
                groups[firstGroupId].RemoveInternalBoundary(edge.SharedBoundaryLength);
            }
        }

        return groups;
    }

    private static Dictionary<(int First, int Second), MutableBoundary> CreateBoundaries(
        RegionAdjacencyGraph fineAdjacency,
        ReadOnlySpan<int> startingFineParents,
        MutableRegionGroup[] groups)
    {
        Dictionary<(int First, int Second), MutableBoundary> boundaries = new();
        foreach (RegionAdjacency edge in fineAdjacency.Edges)
        {
            int firstGroupId = startingFineParents[edge.FirstRegionId];
            int secondGroupId = startingFineParents[edge.SecondRegionId];
            if (firstGroupId == secondGroupId)
            {
                continue;
            }

            (int First, int Second) key = CreateKey(firstGroupId, secondGroupId);
            if (!boundaries.TryGetValue(key, out MutableBoundary? boundary))
            {
                boundary = new MutableBoundary(key.First, key.Second);
                boundaries.Add(key, boundary);
            }

            boundary.Add(edge);
        }

        foreach (MutableBoundary boundary in boundaries.Values)
        {
            groups[boundary.FirstRegionId].Neighbors.Add(boundary.SecondRegionId);
            groups[boundary.SecondRegionId].Neighbors.Add(boundary.FirstRegionId);
        }

        return boundaries;
    }

    private static void MergeGroups(
        MutableRegionGroup first,
        MutableRegionGroup second,
        MutableBoundary sharedBoundary,
        MutableRegionGroup[] groups,
        Dictionary<(int First, int Second), MutableBoundary> boundaries,
        PriorityQueue<MergeCandidate, MergePriority> candidates,
        int[] unionParents,
        RegionMergeSettings settings)
    {
        MutableRegionGroup survivor = first.Id < second.Id ? first : second;
        MutableRegionGroup absorbed = first.Id < second.Id ? second : first;
        boundaries.Remove(CreateKey(survivor.Id, absorbed.Id));

        int[] neighbours = survivor.Neighbors
            .Concat(absorbed.Neighbors)
            .Where(neighbourId => neighbourId != survivor.Id && neighbourId != absorbed.Id)
            .Distinct()
            .OrderBy(neighbourId => neighbourId)
            .ToArray();

        survivor.Merge(absorbed, sharedBoundary.SharedBoundaryLength);
        absorbed.Deactivate();
        unionParents[absorbed.Id] = survivor.Id;
        survivor.Neighbors.Clear();
        absorbed.Neighbors.Clear();

        foreach (int neighbourId in neighbours)
        {
            MutableRegionGroup neighbour = groups[neighbourId];
            neighbour.Neighbors.Remove(survivor.Id);
            neighbour.Neighbors.Remove(absorbed.Id);

            boundaries.Remove(CreateKey(survivor.Id, neighbourId), out MutableBoundary? survivorBoundary);
            boundaries.Remove(CreateKey(absorbed.Id, neighbourId), out MutableBoundary? absorbedBoundary);
            MutableBoundary mergedBoundary = MutableBoundary.Combine(
                survivor.Id,
                neighbourId,
                survivorBoundary,
                absorbedBoundary);
            boundaries.Add(CreateKey(survivor.Id, neighbourId), mergedBoundary);
            survivor.Neighbors.Add(neighbourId);
            neighbour.Neighbors.Add(survivor.Id);
            EnqueueCandidate(candidates, groups, mergedBoundary, settings);
        }
    }

    private static void EnqueueCandidate(
        PriorityQueue<MergeCandidate, MergePriority> candidates,
        MutableRegionGroup[] groups,
        MutableBoundary boundary,
        RegionMergeSettings settings)
    {
        MutableRegionGroup first = groups[boundary.FirstRegionId];
        MutableRegionGroup second = groups[boundary.SecondRegionId];
        if (!first.IsActive
            || !second.IsActive
            || boundary.MaximumBoundaryStrength >= settings.StrongBoundaryThreshold
            || first.NormalizedArea + second.NormalizedArea > settings.MaximumParentAreaFraction)
        {
            return;
        }

        double cost = CalculateCost(first, second, boundary, settings);
        MergeCandidate candidate = new(
            boundary.FirstRegionId,
            boundary.SecondRegionId,
            first.Version,
            second.Version);
        candidates.Enqueue(
            candidate,
            new MergePriority(cost, boundary.FirstRegionId, boundary.SecondRegionId));
    }

    private static double CalculateCost(
        MutableRegionGroup first,
        MutableRegionGroup second,
        MutableBoundary boundary,
        RegionMergeSettings settings)
    {
        return RegionMergeCostModel.Calculate(
            first.PixelCount,
            first.NormalizedArea,
            first.Perimeter,
            first.MeanLightness,
            first.MeanA,
            first.MeanB,
            first.TextureEnergy,
            second.PixelCount,
            second.NormalizedArea,
            second.Perimeter,
            second.MeanLightness,
            second.MeanA,
            second.MeanB,
            second.TextureEnergy,
            boundary.SharedBoundaryLength,
            boundary.MeanBoundaryStrength,
            settings.MaximumParentAreaFraction);
    }

    private static int[] CreateCompactFineParentMapping(
        ReadOnlySpan<int> startingFineParents,
        int[] unionParents)
    {
        Dictionary<int, int> compactParents = new();
        int[] fineParents = new int[startingFineParents.Length];
        for (int fineRegionId = 0; fineRegionId < fineParents.Length; fineRegionId++)
        {
            int rootId = FindRoot(unionParents, startingFineParents[fineRegionId]);
            if (!compactParents.TryGetValue(rootId, out int compactParentId))
            {
                compactParentId = compactParents.Count;
                compactParents.Add(rootId, compactParentId);
            }

            fineParents[fineRegionId] = compactParentId;
        }

        return fineParents;
    }

    private static int FindRoot(int[] unionParents, int groupId)
    {
        int rootId = groupId;
        while (unionParents[rootId] != rootId)
        {
            rootId = unionParents[rootId];
        }

        while (unionParents[groupId] != groupId)
        {
            int nextId = unionParents[groupId];
            unionParents[groupId] = rootId;
            groupId = nextId;
        }

        return rootId;
    }

    private static int CalculateTargetCount(
        int fineRegionCount,
        int currentRegionCount,
        double targetRatio)
    {
        int targetCount = Math.Max(1, (int)Math.Ceiling(fineRegionCount * targetRatio));
        return Math.Min(currentRegionCount, targetCount);
    }

    private static int CountCompactParents(ReadOnlySpan<int> parents)
    {
        int maximumParentId = -1;
        for (int index = 0; index < parents.Length; index++)
        {
            maximumParentId = Math.Max(maximumParentId, parents[index]);
        }

        return checked(maximumParentId + 1);
    }

    private static (int First, int Second) CreateKey(int firstRegionId, int secondRegionId)
    {
        return firstRegionId < secondRegionId
            ? (firstRegionId, secondRegionId)
            : (secondRegionId, firstRegionId);
    }

    private static void EnsureMemoryWithinBudget(int regionCount, int edgeCount)
    {
        long estimatedBytes = checked(
            ((long)regionCount * EstimatedWorkingBytesPerRegion)
            + ((long)edgeCount * EstimatedWorkingBytesPerEdge));
        WorkloadBudgetPolicy.EnsureMemoryWithinBudget(
            estimatedBytes,
            "regional hierarchy construction");
    }

    private static void ValidateInputs(
        IReadOnlyList<ImageRegion> regions,
        RegionAdjacencyGraph adjacency,
        RegionMergeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(regions);
        ArgumentNullException.ThrowIfNull(adjacency);
        ArgumentNullException.ThrowIfNull(settings);
        if (regions.Count == 0)
        {
            throw new ArgumentException("At least one region is required.", nameof(regions));
        }

        if (adjacency.RegionCount != regions.Count)
        {
            throw new ArgumentException(
                "The adjacency graph must use the supplied region count.",
                nameof(adjacency));
        }

        for (int regionId = 0; regionId < regions.Count; regionId++)
        {
            if (regions[regionId].Id != regionId)
            {
                throw new ArgumentException(
                    "Region identifiers must be compact and ordered from zero.",
                    nameof(regions));
            }
        }
    }

    private sealed class MutableRegionGroup
    {
        private readonly int _totalPixelCount;
        private double _aSum;
        private double _bSum;
        private double _lightnessSum;
        private double _textureSum;

        public MutableRegionGroup(int id, int totalPixelCount)
        {
            Id = id;
            _totalPixelCount = totalPixelCount;
        }

        public int Id { get; }

        public int PixelCount { get; private set; }

        public double NormalizedArea => PixelCount / (double)_totalPixelCount;

        public double Perimeter { get; private set; }

        public double MeanLightness => _lightnessSum / PixelCount;

        public double MeanA => _aSum / PixelCount;

        public double MeanB => _bSum / PixelCount;

        public double TextureEnergy => _textureSum / PixelCount;

        public int Version { get; private set; }

        public bool IsActive { get; private set; } = true;

        public HashSet<int> Neighbors { get; } = new();

        public void Add(ImageRegion region)
        {
            PixelCount = checked(PixelCount + region.PixelCount);
            Perimeter += region.Descriptors.Perimeter;
            _lightnessSum += region.Descriptors.MeanLightness * region.PixelCount;
            _aSum += region.Descriptors.MeanA * region.PixelCount;
            _bSum += region.Descriptors.MeanB * region.PixelCount;
            _textureSum += region.Descriptors.TextureEnergy * region.PixelCount;
        }

        public void RemoveInternalBoundary(int sharedBoundaryLength)
        {
            Perimeter = Math.Max(0d, Perimeter - (2d * sharedBoundaryLength));
        }

        public void Merge(MutableRegionGroup other, int sharedBoundaryLength)
        {
            PixelCount = checked(PixelCount + other.PixelCount);
            Perimeter = Math.Max(
                0d,
                Perimeter + other.Perimeter - (2d * sharedBoundaryLength));
            _lightnessSum += other._lightnessSum;
            _aSum += other._aSum;
            _bSum += other._bSum;
            _textureSum += other._textureSum;
            Version++;
        }

        public void Deactivate()
        {
            IsActive = false;
            Version++;
        }
    }

    private sealed class MutableBoundary
    {
        private double _weightedBoundaryStrengthSum;

        public MutableBoundary(int firstRegionId, int secondRegionId)
        {
            (FirstRegionId, SecondRegionId) = CreateKey(firstRegionId, secondRegionId);
        }

        public int FirstRegionId { get; }

        public int SecondRegionId { get; }

        public int SharedBoundaryLength { get; private set; }

        public double MeanBoundaryStrength => _weightedBoundaryStrengthSum / SharedBoundaryLength;

        public double MaximumBoundaryStrength { get; private set; }

        public void Add(RegionAdjacency edge)
        {
            SharedBoundaryLength = checked(SharedBoundaryLength + edge.SharedBoundaryLength);
            _weightedBoundaryStrengthSum += edge.BoundaryStrength * edge.SharedBoundaryLength;
            MaximumBoundaryStrength = Math.Max(MaximumBoundaryStrength, edge.BoundaryStrength);
        }

        public void Add(MutableBoundary boundary)
        {
            SharedBoundaryLength = checked(SharedBoundaryLength + boundary.SharedBoundaryLength);
            _weightedBoundaryStrengthSum += boundary._weightedBoundaryStrengthSum;
            MaximumBoundaryStrength = Math.Max(
                MaximumBoundaryStrength,
                boundary.MaximumBoundaryStrength);
        }

        public static MutableBoundary Combine(
            int firstRegionId,
            int secondRegionId,
            MutableBoundary? firstBoundary,
            MutableBoundary? secondBoundary)
        {
            MutableBoundary combined = new(firstRegionId, secondRegionId);
            if (firstBoundary is not null)
            {
                combined.Add(firstBoundary);
            }

            if (secondBoundary is not null)
            {
                combined.Add(secondBoundary);
            }

            if (combined.SharedBoundaryLength == 0)
            {
                throw new InvalidOperationException("A merged neighbour must retain a shared boundary.");
            }

            return combined;
        }
    }

    private readonly record struct MergeCandidate(
        int FirstRegionId,
        int SecondRegionId,
        int FirstVersion,
        int SecondVersion);

    private readonly record struct MergePriority(
        double Cost,
        int FirstRegionId,
        int SecondRegionId);

    private sealed class MergePriorityComparer : IComparer<MergePriority>
    {
        public static MergePriorityComparer Instance { get; } = new();

        public int Compare(MergePriority first, MergePriority second)
        {
            int costComparison = first.Cost.CompareTo(second.Cost);
            if (costComparison != 0)
            {
                return costComparison;
            }

            int firstComparison = first.FirstRegionId.CompareTo(second.FirstRegionId);
            return firstComparison != 0
                ? firstComparison
                : first.SecondRegionId.CompareTo(second.SecondRegionId);
        }
    }
}
