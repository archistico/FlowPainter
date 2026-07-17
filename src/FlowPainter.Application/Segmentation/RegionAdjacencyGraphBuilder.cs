using FlowPainter.Application.Workloads;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Segmentation;

public static class RegionAdjacencyGraphBuilder
{
    public const int EstimatedWorkingBytesPerRegion = 1024;

    private const int CancellationRowBatch = 16;
    private const double HorizontalTangentRadians = 0d;
    private const double VerticalTangentRadians = Math.PI / 2d;

    public static RegionAdjacencyGraph Build(
        IRgbaPixelSource source,
        RegionLabelMap labels,
        IReadOnlyList<ImageRegion> regions,
        CancellationToken cancellationToken = default)
    {
        ValidateInputs(source, labels, regions);
        EnsureMemoryWithinBudget(labels.RegionCount);
        cancellationToken.ThrowIfCancellationRequested();

        Dictionary<(int First, int Second), BoundaryAccumulator> accumulators = new();
        ImageSize size = labels.Size;
        for (int y = 0; y < size.Height; y++)
        {
            if (y % CancellationRowBatch == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            RegionLabelRow row = labels.GetRow(y);
            for (int x = 0; x + 1 < size.Width; x++)
            {
                int currentRegionId = checked((int)row[x]);
                int rightRegionId = checked((int)row[x + 1]);
                if (rightRegionId != currentRegionId)
                {
                    AccumulateBoundary(
                        accumulators,
                        source,
                        currentRegionId,
                        rightRegionId,
                        x,
                        y,
                        x + 1,
                        y,
                        VerticalTangentRadians);
                }
            }

            if (y + 1 >= size.Height)
            {
                continue;
            }

            RegionLabelRow nextRow = labels.GetRow(y + 1);
            for (int x = 0; x < size.Width; x++)
            {
                int currentRegionId = checked((int)row[x]);
                int lowerRegionId = checked((int)nextRow[x]);
                if (lowerRegionId != currentRegionId)
                {
                    AccumulateBoundary(
                        accumulators,
                        source,
                        currentRegionId,
                        lowerRegionId,
                        x,
                        y,
                        x,
                        y + 1,
                        HorizontalTangentRadians);
                }
            }
        }

        RegionAdjacency[] edges = accumulators
            .OrderBy(pair => pair.Key.First)
            .ThenBy(pair => pair.Key.Second)
            .Select(pair => CreateAdjacency(pair.Value, regions))
            .ToArray();
        return new RegionAdjacencyGraph(labels.RegionCount, edges);
    }

    private static void AccumulateBoundary(
        Dictionary<(int First, int Second), BoundaryAccumulator> accumulators,
        IRgbaPixelSource source,
        int firstObservedRegionId,
        int secondObservedRegionId,
        int firstX,
        int firstY,
        int secondX,
        int secondY,
        double tangentRadians)
    {
        int firstRegionId = Math.Min(firstObservedRegionId, secondObservedRegionId);
        int secondRegionId = Math.Max(firstObservedRegionId, secondObservedRegionId);
        (int First, int Second) key = (firstRegionId, secondRegionId);
        if (!accumulators.TryGetValue(key, out BoundaryAccumulator? accumulator))
        {
            accumulator = new BoundaryAccumulator(firstRegionId, secondRegionId);
            accumulators.Add(key, accumulator);
        }

        LabColorSample firstColor = LabColorConverter.Convert(Sample(source, firstX, firstY));
        LabColorSample secondColor = LabColorConverter.Convert(Sample(source, secondX, secondY));
        accumulator.Add(firstColor.DistanceTo(secondColor), tangentRadians);
    }

    private static RegionAdjacency CreateAdjacency(
        BoundaryAccumulator accumulator,
        IReadOnlyList<ImageRegion> regions)
    {
        RegionVisualDescriptors first = regions[accumulator.FirstRegionId].Descriptors;
        RegionVisualDescriptors second = regions[accumulator.SecondRegionId].Descriptors;
        double lightnessDifference = Math.Abs(first.MeanLightness - second.MeanLightness);
        double colorDifference = CalculateColorDifference(first, second);
        double textureDifference = Math.Abs(first.TextureEnergy - second.TextureEnergy);
        double continuity = accumulator.CalculateContinuity();
        double boundaryStrength = RegionBoundaryStrengthModel.Calculate(
            accumulator.MeanGradient,
            accumulator.MaximumGradient,
            colorDifference,
            textureDifference,
            continuity);

        return new RegionAdjacency(
            accumulator.FirstRegionId,
            accumulator.SecondRegionId,
            accumulator.SharedBoundaryLength,
            accumulator.MeanGradient,
            accumulator.MaximumGradient,
            colorDifference,
            lightnessDifference,
            textureDifference,
            continuity,
            accumulator.CalculatePrevailingTangent(),
            boundaryStrength);
    }

    private static double CalculateColorDifference(
        RegionVisualDescriptors first,
        RegionVisualDescriptors second)
    {
        double lightnessDifference = first.MeanLightness - second.MeanLightness;
        double aDifference = first.MeanA - second.MeanA;
        double bDifference = first.MeanB - second.MeanB;
        return Math.Sqrt(
            (lightnessDifference * lightnessDifference)
            + (aDifference * aDifference)
            + (bDifference * bDifference));
    }

    private static Rgba32 Sample(IRgbaPixelSource source, int x, int y)
    {
        return source.SampleNearest(new NormalizedPoint(
            (x + 0.5d) / source.Size.Width,
            (y + 0.5d) / source.Size.Height));
    }

    private static void EnsureMemoryWithinBudget(int regionCount)
    {
        long estimatedBytes = checked((long)regionCount * EstimatedWorkingBytesPerRegion);
        WorkloadBudgetPolicy.EnsureMemoryWithinBudget(
            estimatedBytes,
            "region adjacency graph construction");
    }

    private static void ValidateInputs(
        IRgbaPixelSource source,
        RegionLabelMap labels,
        IReadOnlyList<ImageRegion> regions)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(regions);

        if (source.Size != labels.Size)
        {
            throw new ArgumentException(
                "The source and label map must have identical dimensions.",
                nameof(labels));
        }

        if (regions.Count != labels.RegionCount)
        {
            throw new ArgumentException(
                "The region list must contain one descriptor entry for every compact label.",
                nameof(regions));
        }

        for (int regionId = 0; regionId < regions.Count; regionId++)
        {
            ImageRegion region = regions[regionId];
            if (region.Id != regionId)
            {
                throw new ArgumentException(
                    "Region identifiers must be compact and ordered from zero.",
                    nameof(regions));
            }
        }
    }

    private sealed class BoundaryAccumulator
    {
        private double _gradientSum;
        private double _orientationCosineSum;
        private double _orientationSineSum;

        public BoundaryAccumulator(int firstRegionId, int secondRegionId)
        {
            FirstRegionId = firstRegionId;
            SecondRegionId = secondRegionId;
        }

        public int FirstRegionId { get; }

        public int SecondRegionId { get; }

        public int SharedBoundaryLength { get; private set; }

        public double MeanGradient => Math.Min(MaximumGradient, _gradientSum / SharedBoundaryLength);

        public double MaximumGradient { get; private set; }

        public void Add(double gradient, double tangentRadians)
        {
            SharedBoundaryLength++;
            _gradientSum += gradient;
            MaximumGradient = Math.Max(MaximumGradient, gradient);
            double doubledTangent = 2d * tangentRadians;
            _orientationCosineSum += Math.Cos(doubledTangent);
            _orientationSineSum += Math.Sin(doubledTangent);
        }

        public double CalculateContinuity()
        {
            double magnitude = Math.Sqrt(
                (_orientationCosineSum * _orientationCosineSum)
                + (_orientationSineSum * _orientationSineSum));
            return Math.Clamp(magnitude / SharedBoundaryLength, 0d, 1d);
        }

        public double CalculatePrevailingTangent()
        {
            if (Math.Abs(_orientationCosineSum) <= double.Epsilon
                && Math.Abs(_orientationSineSum) <= double.Epsilon)
            {
                return 0d;
            }

            double tangent = 0.5d * Math.Atan2(
                _orientationSineSum,
                _orientationCosineSum);
            double normalized = tangent % Math.PI;
            if (normalized < 0d)
            {
                normalized += Math.PI;
            }

            return normalized >= Math.PI ? 0d : normalized;
        }
    }
}
