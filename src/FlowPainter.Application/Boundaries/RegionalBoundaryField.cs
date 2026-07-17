using FlowPainter.Application.Segmentation;
using FlowPainter.Application.Workloads;
using FlowPainter.Domain.Boundaries;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Boundaries;

public sealed class RegionalBoundaryField
{
    public const int EstimatedWorkingBytesPerPixel = 128;
    public const int EstimatedWorkingBytesPerAdjacency = 128;

    private const double DistanceComparisonTolerance = 1e-9d;
    private static readonly (int X, int Y, double Distance)[] NeighborOffsets =
    [
        (-1, 0, 1d),
        (1, 0, 1d),
        (0, -1, 1d),
        (0, 1, 1d),
        (-1, -1, Math.Sqrt(2d)),
        (1, -1, Math.Sqrt(2d)),
        (-1, 1, Math.Sqrt(2d)),
        (1, 1, Math.Sqrt(2d)),
    ];

    private readonly float[] _distancePixels;
    private readonly float[] _boundaryStrength;
    private readonly float[] _influence;
    private readonly bool[] _hardBarriers;
    private readonly BoundaryVector[] _normals;
    private readonly BoundaryVector[] _tangents;
    private readonly int[] _firstRegionIds;
    private readonly int[] _secondRegionIds;

    private RegionalBoundaryField(
        ImageSize size,
        float[] distancePixels,
        float[] boundaryStrength,
        float[] influence,
        bool[] hardBarriers,
        BoundaryVector[] normals,
        BoundaryVector[] tangents,
        int[] firstRegionIds,
        int[] secondRegionIds)
    {
        Size = size;
        _distancePixels = distancePixels;
        _boundaryStrength = boundaryStrength;
        _influence = influence;
        _hardBarriers = hardBarriers;
        _normals = normals;
        _tangents = tangents;
        _firstRegionIds = firstRegionIds;
        _secondRegionIds = secondRegionIds;
    }

    public ImageSize Size { get; }

    public RegionalBoundarySample SampleNearest(NormalizedPoint point)
    {
        int x = Math.Min(Size.Width - 1, checked((int)(point.X * Size.Width)));
        int y = Math.Min(Size.Height - 1, checked((int)(point.Y * Size.Height)));
        return Sample(x, y);
    }

    public RegionalBoundarySample Sample(int x, int y)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(x);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, Size.Width);
        ArgumentOutOfRangeException.ThrowIfNegative(y);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, Size.Height);

        int index = checked((y * Size.Width) + x);
        return new RegionalBoundarySample(
            _distancePixels[index],
            _boundaryStrength[index],
            _influence[index],
            _hardBarriers[index],
            _normals[index],
            _tangents[index],
            _firstRegionIds[index],
            _secondRegionIds[index]);
    }

    public static RegionalBoundaryField Create(
        RegionSegmentationResult segmentation,
        RegionalBoundaryFieldSettings? settings = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(segmentation);
        settings ??= new RegionalBoundaryFieldSettings();
        EnsureMemoryWithinBudget(segmentation);
        cancellationToken.ThrowIfCancellationRequested();

        ImageSize size = segmentation.Labels.Size;
        int pixelCount = checked((int)size.PixelCount);
        float[] distancePixels = Enumerable.Repeat(float.PositiveInfinity, pixelCount).ToArray();
        float[] boundaryStrength = new float[pixelCount];
        float[] influence = new float[pixelCount];
        bool[] hardBarriers = new bool[pixelCount];
        BoundaryVector[] normals = new BoundaryVector[pixelCount];
        BoundaryVector[] tangents = new BoundaryVector[pixelCount];
        int[] firstRegionIds = Enumerable.Repeat(-1, pixelCount).ToArray();
        int[] secondRegionIds = Enumerable.Repeat(-1, pixelCount).ToArray();

        if (segmentation.Adjacency.Edges.Count == 0)
        {
            return new RegionalBoundaryField(
                size,
                distancePixels,
                boundaryStrength,
                influence,
                hardBarriers,
                normals,
                tangents,
                firstRegionIds,
                secondRegionIds);
        }

        int[] bestSeedOrders = Enumerable.Repeat(int.MaxValue, pixelCount).ToArray();
        PriorityQueue<PropagationNode, (double Distance, double NegativeStrength, int SeedOrder, int PixelIndex)> queue = new();
        InitializeBoundarySeeds(
            segmentation,
            distancePixels,
            boundaryStrength,
            normals,
            tangents,
            firstRegionIds,
            secondRegionIds,
            bestSeedOrders,
            queue,
            cancellationToken);
        PropagateNearestBoundaries(
            size,
            settings.MaximumDistancePixels,
            distancePixels,
            boundaryStrength,
            normals,
            tangents,
            firstRegionIds,
            secondRegionIds,
            bestSeedOrders,
            queue,
            cancellationToken);
        CalculateInfluence(
            settings,
            distancePixels,
            boundaryStrength,
            influence,
            hardBarriers,
            cancellationToken);

        return new RegionalBoundaryField(
            size,
            distancePixels,
            boundaryStrength,
            influence,
            hardBarriers,
            normals,
            tangents,
            firstRegionIds,
            secondRegionIds);
    }

    private static void InitializeBoundarySeeds(
        RegionSegmentationResult segmentation,
        float[] distancePixels,
        float[] boundaryStrength,
        BoundaryVector[] normals,
        BoundaryVector[] tangents,
        int[] firstRegionIds,
        int[] secondRegionIds,
        int[] bestSeedOrders,
        PriorityQueue<PropagationNode, (double Distance, double NegativeStrength, int SeedOrder, int PixelIndex)> queue,
        CancellationToken cancellationToken)
    {
        RegionLabelMap labels = segmentation.Labels;
        int nextSeedOrder = 0;
        for (int y = 0; y < labels.Size.Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RegionLabelRow row = labels.GetRow(y);
            for (int x = 0; x < labels.Size.Width; x++)
            {
                int currentRegionId = checked((int)row[x]);
                if (x + 1 < labels.Size.Width)
                {
                    int rightRegionId = checked((int)row[x + 1]);
                    if (rightRegionId != currentRegionId)
                    {
                        AddSeedPair(
                            segmentation.Adjacency,
                            labels.Size.Width,
                            x,
                            y,
                            x + 1,
                            y,
                            currentRegionId,
                            rightRegionId,
                            distancePixels,
                            boundaryStrength,
                            normals,
                            tangents,
                            firstRegionIds,
                            secondRegionIds,
                            bestSeedOrders,
                            queue,
                            ref nextSeedOrder);
                    }
                }

                if (y + 1 >= labels.Size.Height)
                {
                    continue;
                }

                int lowerRegionId = checked((int)labels.GetRow(y + 1)[x]);
                if (lowerRegionId != currentRegionId)
                {
                    AddSeedPair(
                        segmentation.Adjacency,
                        labels.Size.Width,
                        x,
                        y,
                        x,
                        y + 1,
                        currentRegionId,
                        lowerRegionId,
                        distancePixels,
                        boundaryStrength,
                        normals,
                        tangents,
                        firstRegionIds,
                        secondRegionIds,
                        bestSeedOrders,
                        queue,
                        ref nextSeedOrder);
                }
            }
        }
    }

    private static void AddSeedPair(
        RegionAdjacencyGraph adjacency,
        int width,
        int firstX,
        int firstY,
        int secondX,
        int secondY,
        int firstObservedRegionId,
        int secondObservedRegionId,
        float[] distancePixels,
        float[] boundaryStrength,
        BoundaryVector[] normals,
        BoundaryVector[] tangents,
        int[] firstRegionIds,
        int[] secondRegionIds,
        int[] bestSeedOrders,
        PriorityQueue<PropagationNode, (double Distance, double NegativeStrength, int SeedOrder, int PixelIndex)> queue,
        ref int nextSeedOrder)
    {
        if (!adjacency.TryGetEdge(
            firstObservedRegionId,
            secondObservedRegionId,
            out RegionAdjacency? edge)
            || edge is null)
        {
            throw new InvalidOperationException(
                "The regional adjacency graph does not contain an observed label boundary.");
        }

        BoundaryVector tangent = new(
            Math.Cos(edge.PrevailingTangentRadians),
            Math.Sin(edge.PrevailingTangentRadians));
        BoundaryVector firstNormal = OrientNormalToward(
            tangent,
            secondX - firstX,
            secondY - firstY);
        BoundaryVector secondNormal = new(-firstNormal.X, -firstNormal.Y);
        int firstRegionId = Math.Min(firstObservedRegionId, secondObservedRegionId);
        int secondRegionId = Math.Max(firstObservedRegionId, secondObservedRegionId);

        TryAdopt(
            checked((firstY * width) + firstX),
            nextSeedOrder++,
            0d,
            edge.BoundaryStrength,
            firstNormal,
            tangent,
            firstRegionId,
            secondRegionId,
            distancePixels,
            boundaryStrength,
            normals,
            tangents,
            firstRegionIds,
            secondRegionIds,
            bestSeedOrders,
            queue);
        TryAdopt(
            checked((secondY * width) + secondX),
            nextSeedOrder++,
            0d,
            edge.BoundaryStrength,
            secondNormal,
            tangent,
            firstRegionId,
            secondRegionId,
            distancePixels,
            boundaryStrength,
            normals,
            tangents,
            firstRegionIds,
            secondRegionIds,
            bestSeedOrders,
            queue);
    }

    private static BoundaryVector OrientNormalToward(
        BoundaryVector tangent,
        int directionX,
        int directionY)
    {
        BoundaryVector normal = new(-tangent.Y, tangent.X);
        double dot = (normal.X * directionX) + (normal.Y * directionY);
        return dot >= 0d
            ? normal
            : new BoundaryVector(-normal.X, -normal.Y);
    }

    private static void PropagateNearestBoundaries(
        ImageSize size,
        int maximumDistancePixels,
        float[] distancePixels,
        float[] boundaryStrength,
        BoundaryVector[] normals,
        BoundaryVector[] tangents,
        int[] firstRegionIds,
        int[] secondRegionIds,
        int[] bestSeedOrders,
        PriorityQueue<PropagationNode, (double Distance, double NegativeStrength, int SeedOrder, int PixelIndex)> queue,
        CancellationToken cancellationToken)
    {
        int processed = 0;
        while (queue.TryDequeue(out PropagationNode node, out _))
        {
            if ((processed++ & 4095) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (bestSeedOrders[node.PixelIndex] != node.SeedOrder
                || Math.Abs(distancePixels[node.PixelIndex] - node.Distance) > DistanceComparisonTolerance)
            {
                continue;
            }

            int x = node.PixelIndex % size.Width;
            int y = node.PixelIndex / size.Width;
            foreach ((int offsetX, int offsetY, double stepDistance) in NeighborOffsets)
            {
                int neighborX = x + offsetX;
                int neighborY = y + offsetY;
                if (neighborX < 0
                    || neighborX >= size.Width
                    || neighborY < 0
                    || neighborY >= size.Height)
                {
                    continue;
                }

                double candidateDistance = node.Distance + stepDistance;
                if (candidateDistance > maximumDistancePixels + DistanceComparisonTolerance)
                {
                    continue;
                }

                int neighborIndex = checked((neighborY * size.Width) + neighborX);
                TryAdopt(
                    neighborIndex,
                    node.SeedOrder,
                    candidateDistance,
                    boundaryStrength[node.PixelIndex],
                    normals[node.PixelIndex],
                    tangents[node.PixelIndex],
                    firstRegionIds[node.PixelIndex],
                    secondRegionIds[node.PixelIndex],
                    distancePixels,
                    boundaryStrength,
                    normals,
                    tangents,
                    firstRegionIds,
                    secondRegionIds,
                    bestSeedOrders,
                    queue);
            }
        }
    }

    private static void TryAdopt(
        int pixelIndex,
        int seedOrder,
        double candidateDistance,
        double candidateStrength,
        BoundaryVector candidateNormal,
        BoundaryVector candidateTangent,
        int candidateFirstRegionId,
        int candidateSecondRegionId,
        float[] distancePixels,
        float[] boundaryStrength,
        BoundaryVector[] normals,
        BoundaryVector[] tangents,
        int[] firstRegionIds,
        int[] secondRegionIds,
        int[] bestSeedOrders,
        PriorityQueue<PropagationNode, (double Distance, double NegativeStrength, int SeedOrder, int PixelIndex)> queue)
    {
        double currentDistance = distancePixels[pixelIndex];
        double currentStrength = boundaryStrength[pixelIndex];
        bool isCloser = candidateDistance + DistanceComparisonTolerance < currentDistance;
        bool isEquivalentAndStronger = Math.Abs(candidateDistance - currentDistance)
            <= DistanceComparisonTolerance
            && (candidateStrength > currentStrength + DistanceComparisonTolerance
                || (Math.Abs(candidateStrength - currentStrength) <= DistanceComparisonTolerance
                    && seedOrder < bestSeedOrders[pixelIndex]));
        if (!isCloser && !isEquivalentAndStronger)
        {
            return;
        }

        distancePixels[pixelIndex] = (float)candidateDistance;
        boundaryStrength[pixelIndex] = (float)candidateStrength;
        normals[pixelIndex] = candidateNormal;
        tangents[pixelIndex] = candidateTangent;
        firstRegionIds[pixelIndex] = candidateFirstRegionId;
        secondRegionIds[pixelIndex] = candidateSecondRegionId;
        bestSeedOrders[pixelIndex] = seedOrder;
        queue.Enqueue(
            new PropagationNode(pixelIndex, seedOrder, candidateDistance),
            (candidateDistance, -candidateStrength, seedOrder, pixelIndex));
    }

    private static void CalculateInfluence(
        RegionalBoundaryFieldSettings settings,
        float[] distancePixels,
        float[] boundaryStrength,
        float[] influence,
        bool[] hardBarriers,
        CancellationToken cancellationToken)
    {
        float hardBarrierThreshold = (float)settings.HardBarrierThreshold;

        for (int index = 0; index < distancePixels.Length; index++)
        {
            if ((index & 4095) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            double strength = boundaryStrength[index];
            double distance = distancePixels[index];
            if (strength <= 0d || !double.IsFinite(distance))
            {
                continue;
            }

            bool isHard = boundaryStrength[index] >= hardBarrierThreshold;
            double radiusFactor = Lerp(
                1d,
                settings.HardTransitionRadiusFactor,
                strength);
            double effectiveRadius = settings.MaximumDistancePixels * radiusFactor;
            double falloff;
            if (distance <= DistanceComparisonTolerance)
            {
                falloff = 1d;
            }
            else if (effectiveRadius <= 0d || distance >= effectiveRadius)
            {
                falloff = 0d;
            }
            else
            {
                double normalizedDistance = Math.Clamp(distance / effectiveRadius, 0d, 1d);
                double smoothStep = normalizedDistance
                    * normalizedDistance
                    * (3d - (2d * normalizedDistance));
                double exponent = Lerp(
                    settings.SoftTransitionExponent,
                    1d,
                    strength);
                falloff = Math.Pow(1d - smoothStep, exponent);
            }

            influence[index] = (float)Math.Clamp(strength * falloff, 0d, 1d);
            hardBarriers[index] = isHard && falloff > 0d;
        }
    }

    private static double Lerp(double start, double end, double amount)
    {
        return start + ((end - start) * Math.Clamp(amount, 0d, 1d));
    }

    private static void EnsureMemoryWithinBudget(RegionSegmentationResult segmentation)
    {
        long pixelBytes = checked(
            segmentation.Labels.Size.PixelCount * EstimatedWorkingBytesPerPixel);
        long adjacencyBytes = checked(
            (long)segmentation.Adjacency.Edges.Count * EstimatedWorkingBytesPerAdjacency);
        WorkloadBudgetPolicy.EnsureMemoryWithinBudget(
            checked(pixelBytes + adjacencyBytes),
            "regional boundary-field construction");
    }

    private readonly record struct PropagationNode(
        int PixelIndex,
        int SeedOrder,
        double Distance);
}
