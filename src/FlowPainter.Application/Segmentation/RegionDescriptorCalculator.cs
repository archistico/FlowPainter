using FlowPainter.Application.Workloads;
using FlowPainter.Domain.Color;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Segmentation;

public static class RegionDescriptorCalculator
{
    public const double EdgeLightnessThreshold = 2d;
    public const int LightnessBufferBytesPerPixel = sizeof(float);
    public const int EstimatedWorkingBytesPerRegion = 320;

    private const int CancellationRowBatch = 16;
    private const int CancellationRegionBatch = 256;

    public static ImageRegion[] Calculate(
        IRgbaPixelSource source,
        RegionLabelMap labels,
        CancellationToken cancellationToken = default)
    {
        ValidateInputs(source, labels);
        EnsureMemoryWithinBudget(labels);
        cancellationToken.ThrowIfCancellationRequested();

        int regionCount = labels.RegionCount;
        int[] counts = new int[regionCount];
        int[] minimumX = Enumerable.Repeat(labels.Size.Width, regionCount).ToArray();
        int[] minimumY = Enumerable.Repeat(labels.Size.Height, regionCount).ToArray();
        int[] maximumX = Enumerable.Repeat(-1, regionCount).ToArray();
        int[] maximumY = Enumerable.Repeat(-1, regionCount).ToArray();
        int[] perimeters = new int[regionCount];
        int[] edgePixelCounts = new int[regionCount];
        double[] sumX = new double[regionCount];
        double[] sumY = new double[regionCount];
        double[] lightnessSums = new double[regionCount];
        double[] aSums = new double[regionCount];
        double[] bSums = new double[regionCount];
        double[] lightnessSquaredSums = new double[regionCount];
        double[] aSquaredSums = new double[regionCount];
        double[] bSquaredSums = new double[regionCount];
        double[] textureEnergySums = new double[regionCount];
        double[] orientationCosineSums = new double[regionCount];
        double[] orientationSineSums = new double[regionCount];
        float[] lightness = new float[checked((int)labels.Size.PixelCount)];

        AccumulateGeometryAndColor(
            source,
            labels,
            counts,
            minimumX,
            minimumY,
            maximumX,
            maximumY,
            perimeters,
            sumX,
            sumY,
            lightnessSums,
            aSums,
            bSums,
            lightnessSquaredSums,
            aSquaredSums,
            bSquaredSums,
            lightness,
            cancellationToken);

        AccumulateTextureAndOrientation(
            labels,
            lightness,
            textureEnergySums,
            edgePixelCounts,
            orientationCosineSums,
            orientationSineSums,
            cancellationToken);

        return CreateRegions(
            labels,
            counts,
            minimumX,
            minimumY,
            maximumX,
            maximumY,
            perimeters,
            edgePixelCounts,
            sumX,
            sumY,
            lightnessSums,
            aSums,
            bSums,
            lightnessSquaredSums,
            aSquaredSums,
            bSquaredSums,
            textureEnergySums,
            orientationCosineSums,
            orientationSineSums,
            cancellationToken);
    }

    private static void AccumulateGeometryAndColor(
        IRgbaPixelSource source,
        RegionLabelMap labels,
        int[] counts,
        int[] minimumX,
        int[] minimumY,
        int[] maximumX,
        int[] maximumY,
        int[] perimeters,
        double[] sumX,
        double[] sumY,
        double[] lightnessSums,
        double[] aSums,
        double[] bSums,
        double[] lightnessSquaredSums,
        double[] aSquaredSums,
        double[] bSquaredSums,
        float[] lightness,
        CancellationToken cancellationToken)
    {
        ImageSize size = labels.Size;
        for (int y = 0; y < size.Height; y++)
        {
            if (y % CancellationRowBatch == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            RegionLabelRow row = labels.GetRow(y);
            int rowOffset = checked(y * size.Width);
            for (int x = 0; x < size.Width; x++)
            {
                int regionId = checked((int)row[x]);
                LabColorSample sample = LabColorConverter.Convert(Sample(source, x, y));
                int pixelIndex = checked(rowOffset + x);
                lightness[pixelIndex] = (float)sample.Lightness;

                counts[regionId]++;
                minimumX[regionId] = Math.Min(minimumX[regionId], x);
                minimumY[regionId] = Math.Min(minimumY[regionId], y);
                maximumX[regionId] = Math.Max(maximumX[regionId], x);
                maximumY[regionId] = Math.Max(maximumY[regionId], y);
                sumX[regionId] += x + 0.5d;
                sumY[regionId] += y + 0.5d;
                lightnessSums[regionId] += sample.Lightness;
                aSums[regionId] += sample.A;
                bSums[regionId] += sample.B;
                lightnessSquaredSums[regionId] += sample.Lightness * sample.Lightness;
                aSquaredSums[regionId] += sample.A * sample.A;
                bSquaredSums[regionId] += sample.B * sample.B;
                perimeters[regionId] += CountExposedEdges(labels, x, y, row[x]);
            }
        }
    }

    private static void AccumulateTextureAndOrientation(
        RegionLabelMap labels,
        float[] lightness,
        double[] textureEnergySums,
        int[] edgePixelCounts,
        double[] orientationCosineSums,
        double[] orientationSineSums,
        CancellationToken cancellationToken)
    {
        ImageSize size = labels.Size;
        double thresholdSquared = EdgeLightnessThreshold * EdgeLightnessThreshold;
        for (int y = 0; y < size.Height; y++)
        {
            if (y % CancellationRowBatch == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            RegionLabelRow row = labels.GetRow(y);
            int rowOffset = checked(y * size.Width);
            for (int x = 0; x < size.Width; x++)
            {
                int pixelIndex = checked(rowOffset + x);
                int regionId = checked((int)row[x]);
                double center = lightness[pixelIndex];
                double gradientX = SelectStrongestHorizontalDifference(
                    labels,
                    lightness,
                    x,
                    y,
                    regionId,
                    center);
                double gradientY = SelectStrongestVerticalDifference(
                    labels,
                    lightness,
                    x,
                    y,
                    regionId,
                    center);
                double gradientSquared = (gradientX * gradientX) + (gradientY * gradientY);
                textureEnergySums[regionId] += gradientSquared;

                if (gradientSquared < thresholdSquared)
                {
                    continue;
                }

                edgePixelCounts[regionId]++;
                double tangentRadians = NormalizeUndirectedOrientation(
                    Math.Atan2(gradientY, gradientX) + (Math.PI / 2d));
                double doubledOrientation = 2d * tangentRadians;
                orientationCosineSums[regionId] += gradientSquared * Math.Cos(doubledOrientation);
                orientationSineSums[regionId] += gradientSquared * Math.Sin(doubledOrientation);
            }
        }
    }

    private static ImageRegion[] CreateRegions(
        RegionLabelMap labels,
        int[] counts,
        int[] minimumX,
        int[] minimumY,
        int[] maximumX,
        int[] maximumY,
        int[] perimeters,
        int[] edgePixelCounts,
        double[] sumX,
        double[] sumY,
        double[] lightnessSums,
        double[] aSums,
        double[] bSums,
        double[] lightnessSquaredSums,
        double[] aSquaredSums,
        double[] bSquaredSums,
        double[] textureEnergySums,
        double[] orientationCosineSums,
        double[] orientationSineSums,
        CancellationToken cancellationToken)
    {
        ImageRegion[] regions = new ImageRegion[labels.RegionCount];
        for (int regionId = 0; regionId < regions.Length; regionId++)
        {
            if (regionId % CancellationRegionBatch == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            int pixelCount = counts[regionId];
            double meanLightness = lightnessSums[regionId] / pixelCount;
            double meanA = aSums[regionId] / pixelCount;
            double meanB = bSums[regionId] / pixelCount;
            double perimeter = perimeters[regionId];
            double compactness = perimeter <= 0d
                ? 0d
                : Math.Clamp((4d * Math.PI * pixelCount) / (perimeter * perimeter), 0d, 1d);
            double orientation = CalculateDominantOrientation(
                orientationCosineSums[regionId],
                orientationSineSums[regionId]);

            RegionVisualDescriptors descriptors = new(
                perimeter,
                compactness,
                meanLightness,
                meanA,
                meanB,
                CalculateVariance(lightnessSquaredSums[regionId], meanLightness, pixelCount),
                CalculateVariance(aSquaredSums[regionId], meanA, pixelCount),
                CalculateVariance(bSquaredSums[regionId], meanB, pixelCount),
                textureEnergySums[regionId] / pixelCount,
                edgePixelCounts[regionId] / (double)pixelCount,
                orientation);

            regions[regionId] = new ImageRegion(
                regionId,
                pixelCount,
                pixelCount / (double)labels.Size.PixelCount,
                new PixelBounds(
                    minimumX[regionId],
                    minimumY[regionId],
                    checked(maximumX[regionId] + 1),
                    checked(maximumY[regionId] + 1)),
                new RegionCentroid(
                    sumX[regionId] / pixelCount,
                    sumY[regionId] / pixelCount),
                descriptors);
        }

        return regions;
    }

    private static int CountExposedEdges(RegionLabelMap labels, int x, int y, uint regionId)
    {
        int exposedEdges = 0;
        if (x == 0 || labels[x - 1, y] != regionId)
        {
            exposedEdges++;
        }

        if (x + 1 == labels.Size.Width || labels[x + 1, y] != regionId)
        {
            exposedEdges++;
        }

        if (y == 0 || labels[x, y - 1] != regionId)
        {
            exposedEdges++;
        }

        if (y + 1 == labels.Size.Height || labels[x, y + 1] != regionId)
        {
            exposedEdges++;
        }

        return exposedEdges;
    }

    private static double SelectStrongestHorizontalDifference(
        RegionLabelMap labels,
        float[] lightness,
        int x,
        int y,
        int regionId,
        double center)
    {
        double backward = 0d;
        if (x > 0 && labels[x - 1, y] == (uint)regionId)
        {
            backward = center - lightness[checked((y * labels.Size.Width) + x - 1)];
        }

        double forward = 0d;
        if (x + 1 < labels.Size.Width && labels[x + 1, y] == (uint)regionId)
        {
            forward = lightness[checked((y * labels.Size.Width) + x + 1)] - center;
        }

        return Math.Abs(forward) >= Math.Abs(backward) ? forward : backward;
    }

    private static double SelectStrongestVerticalDifference(
        RegionLabelMap labels,
        float[] lightness,
        int x,
        int y,
        int regionId,
        double center)
    {
        double backward = 0d;
        if (y > 0 && labels[x, y - 1] == (uint)regionId)
        {
            backward = center - lightness[checked(((y - 1) * labels.Size.Width) + x)];
        }

        double forward = 0d;
        if (y + 1 < labels.Size.Height && labels[x, y + 1] == (uint)regionId)
        {
            forward = lightness[checked(((y + 1) * labels.Size.Width) + x)] - center;
        }

        return Math.Abs(forward) >= Math.Abs(backward) ? forward : backward;
    }

    private static double CalculateVariance(double squaredSum, double mean, int count)
    {
        return Math.Max(0d, (squaredSum / count) - (mean * mean));
    }

    private static double CalculateDominantOrientation(double cosineSum, double sineSum)
    {
        if (Math.Abs(cosineSum) <= double.Epsilon && Math.Abs(sineSum) <= double.Epsilon)
        {
            return 0d;
        }

        return NormalizeUndirectedOrientation(0.5d * Math.Atan2(sineSum, cosineSum));
    }

    private static double NormalizeUndirectedOrientation(double radians)
    {
        double normalized = radians % Math.PI;
        if (normalized < 0d)
        {
            normalized += Math.PI;
        }

        return normalized >= Math.PI ? 0d : normalized;
    }

    private static Rgba32 Sample(IRgbaPixelSource source, int x, int y)
    {
        return source.SampleNearest(new NormalizedPoint(
            (x + 0.5d) / source.Size.Width,
            (y + 0.5d) / source.Size.Height));
    }

    private static void EnsureMemoryWithinBudget(RegionLabelMap labels)
    {
        long lightnessBytes = labels.Size.GetRequiredBytes(LightnessBufferBytesPerPixel);
        long regionBytes = checked((long)labels.RegionCount * EstimatedWorkingBytesPerRegion);
        WorkloadBudgetPolicy.EnsureMemoryWithinBudget(
            checked(lightnessBytes + regionBytes),
            "regional descriptor calculation");
    }

    private static void ValidateInputs(IRgbaPixelSource source, RegionLabelMap labels)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(labels);
        if (source.Size != labels.Size)
        {
            throw new ArgumentException(
                "The source and label map must have identical dimensions.",
                nameof(labels));
        }
    }

}
