using FlowPainter.Domain.Images;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Segmentation;

public static class RegionConnectivityNormalizer
{
    private const int CancellationCheckInterval = 65_536;

    public static RegionConnectivityResult Normalize(
        ImageSize size,
        int rawRegionCount,
        ReadOnlySpan<int> rawLabels,
        int minimumRegionPixelCount,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rawRegionCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimumRegionPixelCount);
        if (rawRegionCount > size.PixelCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rawRegionCount),
                rawRegionCount,
                "The number of raw regions cannot exceed the number of pixels.");
        }

        if (rawLabels.Length != size.PixelCount)
        {
            throw new ArgumentException(
                $"Expected {size.PixelCount} raw labels but received {rawLabels.Length}.",
                nameof(rawLabels));
        }

        cancellationToken.ThrowIfCancellationRequested();
        int pixelCount = checked((int)size.PixelCount);
        int[] componentLabels = new int[pixelCount];
        Array.Fill(componentLabels, -1);
        int[] queue = new int[pixelCount];
        bool[] rawLabelsUsed = new bool[rawRegionCount];
        List<int> componentSizes = [];

        for (int startIndex = 0; startIndex < pixelCount; startIndex++)
        {
            if (startIndex % CancellationCheckInterval == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (componentLabels[startIndex] >= 0)
            {
                continue;
            }

            int rawLabel = rawLabels[startIndex];
            ValidateRawLabel(rawLabel, rawRegionCount, nameof(rawLabels));
            rawLabelsUsed[rawLabel] = true;
            int componentId = componentSizes.Count;
            int head = 0;
            int tail = 0;
            queue[tail++] = startIndex;
            componentLabels[startIndex] = componentId;
            int sizeInPixels = 0;

            while (head < tail)
            {
                if (head % CancellationCheckInterval == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                int currentIndex = queue[head++];
                sizeInPixels++;
                int x = currentIndex % size.Width;
                int y = currentIndex / size.Width;

                TryEnqueueNeighbor(
                    rawLabels,
                    componentLabels,
                    queue,
                    ref tail,
                    currentIndex - 1,
                    x > 0,
                    rawLabel,
                    rawRegionCount,
                    componentId);
                TryEnqueueNeighbor(
                    rawLabels,
                    componentLabels,
                    queue,
                    ref tail,
                    currentIndex + 1,
                    x + 1 < size.Width,
                    rawLabel,
                    rawRegionCount,
                    componentId);
                TryEnqueueNeighbor(
                    rawLabels,
                    componentLabels,
                    queue,
                    ref tail,
                    currentIndex - size.Width,
                    y > 0,
                    rawLabel,
                    rawRegionCount,
                    componentId);
                TryEnqueueNeighbor(
                    rawLabels,
                    componentLabels,
                    queue,
                    ref tail,
                    currentIndex + size.Width,
                    y + 1 < size.Height,
                    rawLabel,
                    rawRegionCount,
                    componentId);
            }

            componentSizes.Add(sizeInPixels);
        }

        int usedRawRegionCount = rawLabelsUsed.Count(isUsed => isUsed);
        int rawComponentCount = componentSizes.Count;
        int disconnectedComponentsRepaired = checked(rawComponentCount - usedRawRegionCount);
        Dictionary<int, int>[] adjacency = BuildAdjacency(
            size,
            componentLabels,
            rawComponentCount,
            cancellationToken);
        int undersizedComponentsMerged = MergeUndersizedComponents(
            componentLabels,
            componentSizes,
            adjacency,
            minimumRegionPixelCount,
            cancellationToken);
        int finalRegionCount = CompactRootLabels(componentLabels, componentSizes.Count, cancellationToken);
        RegionLabelMap labels = RegionLabelMap.Create(size, finalRegionCount, componentLabels);

        return new RegionConnectivityResult(
            labels,
            rawComponentCount,
            disconnectedComponentsRepaired,
            undersizedComponentsMerged);
    }

    private static void TryEnqueueNeighbor(
        ReadOnlySpan<int> rawLabels,
        int[] componentLabels,
        int[] queue,
        ref int tail,
        int neighborIndex,
        bool isInside,
        int rawLabel,
        int rawRegionCount,
        int componentId)
    {
        if (!isInside || componentLabels[neighborIndex] >= 0)
        {
            return;
        }

        int neighborLabel = rawLabels[neighborIndex];
        ValidateRawLabel(neighborLabel, rawRegionCount, nameof(rawLabels));
        if (neighborLabel != rawLabel)
        {
            return;
        }

        componentLabels[neighborIndex] = componentId;
        queue[tail++] = neighborIndex;
    }

    private static Dictionary<int, int>[] BuildAdjacency(
        ImageSize size,
        int[] componentLabels,
        int componentCount,
        CancellationToken cancellationToken)
    {
        Dictionary<int, int>[] adjacency = new Dictionary<int, int>[componentCount];
        for (int componentId = 0; componentId < componentCount; componentId++)
        {
            adjacency[componentId] = [];
        }

        for (int y = 0; y < size.Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int rowOffset = checked(y * size.Width);
            for (int x = 0; x < size.Width; x++)
            {
                int index = rowOffset + x;
                int componentId = componentLabels[index];
                if (x + 1 < size.Width)
                {
                    AddBoundary(adjacency, componentId, componentLabels[index + 1]);
                }

                if (y + 1 < size.Height)
                {
                    AddBoundary(adjacency, componentId, componentLabels[index + size.Width]);
                }
            }
        }

        return adjacency;
    }

    private static void AddBoundary(
        Dictionary<int, int>[] adjacency,
        int firstComponentId,
        int secondComponentId)
    {
        if (firstComponentId == secondComponentId)
        {
            return;
        }

        IncrementBoundary(adjacency[firstComponentId], secondComponentId, 1);
        IncrementBoundary(adjacency[secondComponentId], firstComponentId, 1);
    }

    private static int MergeUndersizedComponents(
        int[] componentLabels,
        List<int> componentSizes,
        Dictionary<int, int>[] adjacency,
        int minimumRegionPixelCount,
        CancellationToken cancellationToken)
    {
        int componentCount = componentSizes.Count;
        int[] parents = new int[componentCount];
        bool[] active = new bool[componentCount];
        int[] sizes = componentSizes.ToArray();
        PriorityQueue<int, (int Size, int Id)> queue = new();

        for (int componentId = 0; componentId < componentCount; componentId++)
        {
            parents[componentId] = componentId;
            active[componentId] = true;
            if (sizes[componentId] < minimumRegionPixelCount)
            {
                queue.Enqueue(componentId, (sizes[componentId], componentId));
            }
        }

        int mergedCount = 0;
        while (queue.TryDequeue(out int sourceId, out _))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!active[sourceId] || sizes[sourceId] >= minimumRegionPixelCount)
            {
                continue;
            }

            int targetId = SelectMergeTarget(sourceId, active, sizes, adjacency);
            if (targetId < 0)
            {
                continue;
            }

            MergeComponent(sourceId, targetId, parents, active, sizes, adjacency);
            mergedCount++;
            if (sizes[targetId] < minimumRegionPixelCount)
            {
                queue.Enqueue(targetId, (sizes[targetId], targetId));
            }
        }

        for (int index = 0; index < componentLabels.Length; index++)
        {
            if (index % CancellationCheckInterval == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            componentLabels[index] = ResolveRoot(componentLabels[index], parents);
        }

        return mergedCount;
    }

    private static int SelectMergeTarget(
        int sourceId,
        bool[] active,
        int[] sizes,
        Dictionary<int, int>[] adjacency)
    {
        int bestTargetId = -1;
        int bestBoundaryLength = -1;
        int bestTargetSize = -1;

        foreach ((int candidateId, int boundaryLength) in adjacency[sourceId])
        {
            if (!active[candidateId])
            {
                continue;
            }

            int candidateSize = sizes[candidateId];
            bool isBetter = boundaryLength > bestBoundaryLength
                || (boundaryLength == bestBoundaryLength && candidateSize > bestTargetSize)
                || (boundaryLength == bestBoundaryLength
                    && candidateSize == bestTargetSize
                    && (bestTargetId < 0 || candidateId < bestTargetId));
            if (!isBetter)
            {
                continue;
            }

            bestTargetId = candidateId;
            bestBoundaryLength = boundaryLength;
            bestTargetSize = candidateSize;
        }

        return bestTargetId;
    }

    private static void MergeComponent(
        int sourceId,
        int targetId,
        int[] parents,
        bool[] active,
        int[] sizes,
        Dictionary<int, int>[] adjacency)
    {
        KeyValuePair<int, int>[] sourceNeighbors = adjacency[sourceId].ToArray();
        foreach ((int neighborId, int boundaryLength) in sourceNeighbors)
        {
            adjacency[neighborId].Remove(sourceId);
            if (!active[neighborId] || neighborId == targetId)
            {
                continue;
            }

            IncrementBoundary(adjacency[targetId], neighborId, boundaryLength);
            IncrementBoundary(adjacency[neighborId], targetId, boundaryLength);
        }

        adjacency[targetId].Remove(sourceId);
        adjacency[sourceId].Clear();
        parents[sourceId] = targetId;
        active[sourceId] = false;
        sizes[targetId] = checked(sizes[targetId] + sizes[sourceId]);
        sizes[sourceId] = 0;
    }

    private static void IncrementBoundary(
        Dictionary<int, int> boundaries,
        int neighborId,
        int amount)
    {
        boundaries.TryGetValue(neighborId, out int current);
        boundaries[neighborId] = checked(current + amount);
    }

    private static int CompactRootLabels(
        int[] componentLabels,
        int componentCount,
        CancellationToken cancellationToken)
    {
        int[] remap = new int[componentCount];
        Array.Fill(remap, -1);
        int regionCount = 0;
        for (int index = 0; index < componentLabels.Length; index++)
        {
            if (index % CancellationCheckInterval == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            int rootId = componentLabels[index];
            int compactId = remap[rootId];
            if (compactId < 0)
            {
                compactId = regionCount++;
                remap[rootId] = compactId;
            }

            componentLabels[index] = compactId;
        }

        return regionCount;
    }

    private static int ResolveRoot(int componentId, int[] parents)
    {
        int rootId = componentId;
        while (parents[rootId] != rootId)
        {
            rootId = parents[rootId];
        }

        while (parents[componentId] != componentId)
        {
            int nextId = parents[componentId];
            parents[componentId] = rootId;
            componentId = nextId;
        }

        return rootId;
    }

    private static void ValidateRawLabel(int label, int rawRegionCount, string parameterName)
    {
        if (label < 0 || label >= rawRegionCount)
        {
            throw new ArgumentException(
                $"Raw label {label} is outside the range 0 to {rawRegionCount - 1}.",
                parameterName);
        }
    }
}
