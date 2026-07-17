using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Segmentation;

public static class RegionalStructureAnalysisComposer
{
    public const string ProviderIdentifier = "slic-regional-structure-v1";
    private const double BoundaryWeight = 0.20d;
    private const double StructuralWeight = 0.55d;
    private const double TextureWeight = 0.15d;
    private const double SpecificityWeight = 0.10d;

    public static RegionalStructureAnalysisResult Compose(
        RegionSegmentationResult segmentation,
        DetailMap structuralDetailMap,
        IEnumerable<RegionRoleOverride>? roleOverrides,
        double transitionWidth,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(segmentation);
        ArgumentNullException.ThrowIfNull(structuralDetailMap);
        ValidateTransitionWidth(transitionWidth);
        if (segmentation.Labels.Size != structuralDetailMap.Size)
        {
            throw new ArgumentException(
                "The regional segmentation and structural detail map must have identical dimensions.",
                nameof(structuralDetailMap));
        }

        cancellationToken.ThrowIfCancellationRequested();
        RegionRoleOverride[] overrides = roleOverrides?.ToArray() ?? [];
        ValidateOverrides(overrides);

        int width = structuralDetailMap.Width;
        int height = structuralDetailMap.Height;
        int length = checked(width * height);
        float[] structural = structuralDetailMap.CopyValues();
        float[] saliency = new float[length];
        float[] protection = new float[length];
        float[] boundaries = new float[length];
        float[] focus = new float[length];
        float[] importance = new float[length];
        float[] backgroundRoles = new float[length];
        float[] ignored = new float[length];

        RegionSignals signals = CalculateRegionSignals(
            segmentation,
            structural,
            cancellationToken);
        PopulateRegionalMaps(
            segmentation.Labels,
            structural,
            signals,
            saliency,
            protection,
            focus,
            importance,
            cancellationToken);
        RasterizeBoundaryEvidence(
            segmentation,
            boundaries,
            cancellationToken);
        ApplyRoleOverrides(
            overrides,
            width,
            height,
            transitionWidth,
            saliency,
            protection,
            boundaries,
            focus,
            importance,
            backgroundRoles,
            ignored,
            cancellationToken);

        return new RegionalStructureAnalysisResult(
            new DetailMap(width, height, saliency),
            new DetailMap(width, height, protection),
            new DetailMap(width, height, boundaries),
            new DetailMap(width, height, focus),
            new DetailMap(width, height, importance),
            new DetailMap(width, height, backgroundRoles),
            new DetailMap(width, height, ignored),
            overrides,
            ProviderIdentifier);
    }

    private static RegionSignals CalculateRegionSignals(
        RegionSegmentationResult segmentation,
        ReadOnlySpan<float> structural,
        CancellationToken cancellationToken)
    {
        int regionCount = segmentation.Labels.RegionCount;
        double[] structuralSums = new double[regionCount];
        double[] boundarySums = new double[regionCount];
        int[] boundaryLengths = new int[regionCount];
        double[] maximumBoundaries = new double[regionCount];
        int width = segmentation.Labels.Size.Width;

        for (int y = 0; y < segmentation.Labels.Size.Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RegionLabelRow row = segmentation.Labels.GetRow(y);
            int rowOffset = checked(y * width);
            for (int x = 0; x < width; x++)
            {
                int regionId = checked((int)row[x]);
                structuralSums[regionId] += structural[rowOffset + x];
            }
        }

        int edgeIndex = 0;
        foreach (RegionAdjacency edge in segmentation.Adjacency.Edges)
        {
            if ((edgeIndex++ & 4095) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            double weighted = edge.BoundaryStrength * edge.SharedBoundaryLength;
            boundarySums[edge.FirstRegionId] += weighted;
            boundarySums[edge.SecondRegionId] += weighted;
            boundaryLengths[edge.FirstRegionId] = checked(
                boundaryLengths[edge.FirstRegionId] + edge.SharedBoundaryLength);
            boundaryLengths[edge.SecondRegionId] = checked(
                boundaryLengths[edge.SecondRegionId] + edge.SharedBoundaryLength);
            maximumBoundaries[edge.FirstRegionId] = Math.Max(
                maximumBoundaries[edge.FirstRegionId],
                edge.BoundaryStrength);
            maximumBoundaries[edge.SecondRegionId] = Math.Max(
                maximumBoundaries[edge.SecondRegionId],
                edge.BoundaryStrength);
        }

        RegionHierarchyLevel broadLevel = segmentation.Hierarchy.Levels[^1];
        long[] broadPixelCounts = new long[broadLevel.ParentRegionCount];
        int regionIndex = 0;
        foreach (ImageRegion region in segmentation.Regions)
        {
            if ((regionIndex++ & 4095) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            int broadParent = broadLevel.GetParentId(region.Id);
            broadPixelCounts[broadParent] = checked(broadPixelCounts[broadParent] + region.PixelCount);
        }

        double totalPixels = segmentation.Labels.Size.PixelCount;
        float[] regionSaliency = new float[regionCount];
        float[] regionProtection = new float[regionCount];
        float[] regionFocus = new float[regionCount];
        float[] regionImportance = new float[regionCount];
        regionIndex = 0;
        foreach (ImageRegion region in segmentation.Regions)
        {
            if ((regionIndex++ & 4095) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            int regionId = region.Id;
            double structuralMean = structuralSums[regionId] / region.PixelCount;
            double meanBoundary = boundaryLengths[regionId] == 0
                ? 0d
                : boundarySums[regionId] / boundaryLengths[regionId];
            double boundary = Math.Clamp(
                (0.65d * maximumBoundaries[regionId]) + (0.35d * meanBoundary),
                0d,
                1d);
            RegionVisualDescriptors descriptors = region.Descriptors;
            double texture = Math.Clamp(
                (0.60d * descriptors.EdgeDensity)
                + (0.40d * NormalizePositive(descriptors.TextureEnergy)),
                0d,
                1d);
            int broadParent = broadLevel.GetParentId(regionId);
            double broadArea = broadPixelCounts[broadParent] / totalPixels;
            double specificity = Math.Clamp(1d - Math.Sqrt(broadArea), 0d, 1d);
            double regionalImportance = Math.Clamp(
                (StructuralWeight * structuralMean)
                + (BoundaryWeight * boundary)
                + (TextureWeight * texture)
                + (SpecificityWeight * specificity),
                0d,
                1d);
            double regionalSaliency = Math.Clamp(
                Math.Max(structuralMean, (0.55d * regionalImportance) + (0.45d * boundary)),
                0d,
                1d);
            double focalSignal = SmoothThreshold(regionalImportance, 0.52d, 0.88d)
                * (0.60d + (0.40d * specificity));

            regionSaliency[regionId] = (float)regionalSaliency;
            regionImportance[regionId] = (float)regionalImportance;
            regionFocus[regionId] = (float)Math.Clamp(focalSignal, 0d, 1d);
            regionProtection[regionId] = (float)Math.Clamp(focalSignal * 0.65d, 0d, 1d);
        }

        return new RegionSignals(
            regionSaliency,
            regionProtection,
            regionFocus,
            regionImportance);
    }

    private static void PopulateRegionalMaps(
        RegionLabelMap labels,
        ReadOnlySpan<float> structural,
        RegionSignals signals,
        Span<float> saliency,
        Span<float> protection,
        Span<float> focus,
        Span<float> importance,
        CancellationToken cancellationToken)
    {
        int width = labels.Size.Width;
        for (int y = 0; y < labels.Size.Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RegionLabelRow row = labels.GetRow(y);
            int rowOffset = checked(y * width);
            for (int x = 0; x < width; x++)
            {
                int index = rowOffset + x;
                int regionId = checked((int)row[x]);
                float localStructural = structural[index];
                saliency[index] = Math.Max(
                    localStructural,
                    signals.Saliency[regionId]);
                protection[index] = signals.Protection[regionId];
                focus[index] = signals.Focus[regionId];
                importance[index] = (float)Math.Clamp(
                    (0.55d * localStructural) + (0.45d * signals.Importance[regionId]),
                    0d,
                    1d);
            }
        }
    }

    private static void RasterizeBoundaryEvidence(
        RegionSegmentationResult segmentation,
        Span<float> boundaries,
        CancellationToken cancellationToken)
    {
        RegionLabelMap labels = segmentation.Labels;
        int width = labels.Size.Width;
        for (int y = 0; y < labels.Size.Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RegionLabelRow row = labels.GetRow(y);
            int rowOffset = checked(y * width);
            for (int x = 0; x < width; x++)
            {
                int index = rowOffset + x;
                int regionId = checked((int)row[x]);
                if (x + 1 < width)
                {
                    AddBoundaryEvidence(
                        segmentation.Adjacency,
                        regionId,
                        checked((int)row[x + 1]),
                        index,
                        index + 1,
                        boundaries);
                }

                if (y + 1 < labels.Size.Height)
                {
                    RegionLabelRow nextRow = labels.GetRow(y + 1);
                    AddBoundaryEvidence(
                        segmentation.Adjacency,
                        regionId,
                        checked((int)nextRow[x]),
                        index,
                        index + width,
                        boundaries);
                }
            }
        }
    }

    private static void AddBoundaryEvidence(
        RegionAdjacencyGraph adjacency,
        int firstRegionId,
        int secondRegionId,
        int firstIndex,
        int secondIndex,
        Span<float> boundaries)
    {
        if (firstRegionId == secondRegionId)
        {
            return;
        }

        if (!adjacency.TryGetEdge(firstRegionId, secondRegionId, out RegionAdjacency? edge)
            || edge is null)
        {
            throw new InvalidOperationException("The label map contains an adjacency missing from the regional graph.");
        }

        float strength = (float)edge.BoundaryStrength;
        boundaries[firstIndex] = Math.Max(boundaries[firstIndex], strength);
        boundaries[secondIndex] = Math.Max(boundaries[secondIndex], strength);
    }

    private static void ApplyRoleOverrides(
        ReadOnlySpan<RegionRoleOverride> overrides,
        int width,
        int height,
        double transitionWidth,
        Span<float> saliency,
        Span<float> protection,
        Span<float> boundaries,
        Span<float> focus,
        Span<float> importance,
        Span<float> backgroundRoles,
        Span<float> ignored,
        CancellationToken cancellationToken)
    {
        if (overrides.IsEmpty)
        {
            return;
        }

        int length = checked(width * height);
        float[][] influences = new float[Enum.GetValues<RegionRole>().Length][];
        for (int index = 0; index < influences.Length; index++)
        {
            influences[index] = new float[length];
        }

        foreach (RegionRoleOverride roleOverride in overrides)
        {
            cancellationToken.ThrowIfCancellationRequested();
            MergeOverrideInfluence(
                influences[(int)roleOverride.Role],
                width,
                height,
                roleOverride,
                transitionWidth,
                cancellationToken);
        }

        for (int index = 0; index < length; index++)
        {
            if ((index & 4095) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            float supporting = influences[(int)RegionRole.Supporting][index];
            float background = influences[(int)RegionRole.Background][index];
            float ignore = influences[(int)RegionRole.Ignore][index];
            float subject = influences[(int)RegionRole.Subject][index];
            float focal = influences[(int)RegionRole.Focal][index];
            float critical = influences[(int)RegionRole.CriticalDetail][index];

            if (supporting > 0f)
            {
                importance[index] = Promote(importance[index], supporting * 0.45f);
            }

            if (background > 0f)
            {
                backgroundRoles[index] = Promote(backgroundRoles[index], background);
                float retention = 1f - background;
                saliency[index] *= retention;
                protection[index] *= retention;
                focus[index] *= retention;
                importance[index] *= retention;
            }

            if (ignore > 0f)
            {
                ignored[index] = Promote(ignored[index], ignore);
                float retention = 1f - ignore;
                saliency[index] *= retention;
                protection[index] *= retention;
                boundaries[index] *= retention;
                focus[index] *= retention;
                importance[index] *= retention;
            }

            if (subject > 0f)
            {
                protection[index] = Promote(protection[index], subject);
                importance[index] = Promote(importance[index], subject * 0.85f);
                focus[index] = Promote(focus[index], subject * 0.25f);
            }

            if (focal > 0f)
            {
                protection[index] = Promote(protection[index], focal);
                focus[index] = Promote(focus[index], focal);
                importance[index] = Promote(importance[index], focal);
            }

            if (critical > 0f)
            {
                saliency[index] = Promote(saliency[index], critical);
                protection[index] = Promote(protection[index], critical);
                focus[index] = Promote(focus[index], critical);
                importance[index] = Promote(importance[index], critical);
            }
        }
    }

    private static void MergeOverrideInfluence(
        Span<float> target,
        int width,
        int height,
        RegionRoleOverride roleOverride,
        double transitionWidth,
        CancellationToken cancellationToken)
    {
        double transitionPixels = transitionWidth * Math.Min(width, height);
        double left = roleOverride.Bounds.Left * width;
        double top = roleOverride.Bounds.Top * height;
        double right = roleOverride.Bounds.Right * width;
        double bottom = roleOverride.Bounds.Bottom * height;
        int minimumX = Math.Clamp((int)Math.Floor(left - transitionPixels), 0, width - 1);
        int maximumX = Math.Clamp((int)Math.Ceiling(right + transitionPixels) - 1, 0, width - 1);
        int minimumY = Math.Clamp((int)Math.Floor(top - transitionPixels), 0, height - 1);
        int maximumY = Math.Clamp((int)Math.Ceiling(bottom + transitionPixels) - 1, 0, height - 1);

        for (int y = minimumY; y <= maximumY; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            double pixelY = y + 0.5d;
            for (int x = minimumX; x <= maximumX; x++)
            {
                double influence = CalculateFeatherInfluence(
                    x + 0.5d,
                    pixelY,
                    left,
                    top,
                    right,
                    bottom,
                    transitionPixels);
                if (influence <= 0d)
                {
                    continue;
                }

                int index = checked((y * width) + x);
                target[index] = Math.Max(target[index], (float)influence);
            }
        }
    }

    private static double CalculateFeatherInfluence(
        double x,
        double y,
        double left,
        double top,
        double right,
        double bottom,
        double transitionPixels)
    {
        bool inside = x >= left && x <= right && y >= top && y <= bottom;
        if (transitionPixels <= 0d)
        {
            return inside ? 1d : 0d;
        }

        if (inside)
        {
            double distanceToBoundary = Math.Min(
                Math.Min(x - left, right - x),
                Math.Min(y - top, bottom - y));
            double maximumInnerDistance = Math.Min(
                (right - left) * 0.5d,
                (bottom - top) * 0.5d);
            double innerTransition = Math.Min(transitionPixels, maximumInnerDistance);
            if (innerTransition <= 0d)
            {
                return 1d;
            }

            double amount = Math.Clamp(distanceToBoundary / innerTransition, 0d, 1d);
            return 0.5d + (0.5d * SmoothStep(amount));
        }

        double distanceX = x < left
            ? left - x
            : x > right
                ? x - right
                : 0d;
        double distanceY = y < top
            ? top - y
            : y > bottom
                ? y - bottom
                : 0d;
        double distanceOutside = Math.Sqrt((distanceX * distanceX) + (distanceY * distanceY));
        double outsideAmount = Math.Clamp(distanceOutside / transitionPixels, 0d, 1d);
        return 0.5d * (1d - SmoothStep(outsideAmount));
    }

    private static float Promote(float current, float influence)
    {
        return current + (influence * (1f - current));
    }

    private static double NormalizePositive(double value)
    {
        return value <= 0d ? 0d : value / (1d + value);
    }

    private static double SmoothThreshold(double value, double minimum, double maximum)
    {
        double amount = Math.Clamp((value - minimum) / (maximum - minimum), 0d, 1d);
        return SmoothStep(amount);
    }

    private static double SmoothStep(double amount)
    {
        return amount * amount * (3d - (2d * amount));
    }

    private static void ValidateOverrides(ReadOnlySpan<RegionRoleOverride> overrides)
    {
        HashSet<string> identifiers = new(StringComparer.OrdinalIgnoreCase);
        foreach (RegionRoleOverride roleOverride in overrides)
        {
            if (!identifiers.Add(roleOverride.Id))
            {
                throw new ArgumentException(
                    $"Duplicate region-role override identifier '{roleOverride.Id}'.",
                    nameof(overrides));
            }
        }
    }

    private static void ValidateTransitionWidth(double transitionWidth)
    {
        if (!double.IsFinite(transitionWidth) || transitionWidth < 0d || transitionWidth > 0.5d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(transitionWidth),
                transitionWidth,
                "Transition width must be finite and between 0 and 0.5 of the shorter map dimension.");
        }
    }

    private sealed record RegionSignals(
        float[] Saliency,
        float[] Protection,
        float[] Focus,
        float[] Importance);
}
