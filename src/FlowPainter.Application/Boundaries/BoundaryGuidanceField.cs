using FlowPainter.Application.Segmentation;
using FlowPainter.Domain.Boundaries;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Boundaries;

public sealed class BoundaryGuidanceField
{
    private readonly float[] _influence;
    private readonly float[] _hardness;
    private readonly float[] _subjectBoundary;
    private readonly float[] _cornerStrength;
    private readonly BoundaryVector[] _tangents;
    private readonly float[] _regionalBoundaryStrength;
    private readonly float[] _regionalDistancePixels;
    private readonly BoundaryVector[] _normals;
    private readonly bool[] _hardBarriers;

    private BoundaryGuidanceField(
        ImageSize size,
        float[] influence,
        float[] hardness,
        float[] subjectBoundary,
        float[] cornerStrength,
        BoundaryVector[] tangents,
        float[] regionalBoundaryStrength,
        float[] regionalDistancePixels,
        BoundaryVector[] normals,
        bool[] hardBarriers)
    {
        Size = size;
        _influence = influence;
        _hardness = hardness;
        _subjectBoundary = subjectBoundary;
        _cornerStrength = cornerStrength;
        _tangents = tangents;
        _regionalBoundaryStrength = regionalBoundaryStrength;
        _regionalDistancePixels = regionalDistancePixels;
        _normals = normals;
        _hardBarriers = hardBarriers;
    }

    public ImageSize Size { get; }

    public BoundaryGuidanceSample SampleNearest(NormalizedPoint point)
    {
        int x = Math.Min(Size.Width - 1, checked((int)(point.X * Size.Width)));
        int y = Math.Min(Size.Height - 1, checked((int)(point.Y * Size.Height)));
        int index = checked((y * Size.Width) + x);
        return new BoundaryGuidanceSample(
            _influence[index],
            _hardness[index],
            _subjectBoundary[index],
            _cornerStrength[index],
            _tangents[index],
            _regionalBoundaryStrength[index],
            _regionalDistancePixels[index],
            _normals[index],
            _hardBarriers[index]);
    }

    public DetailMap CreateReinforcedDetailMap(
        DetailMap detailMap,
        double contourReinforcement)
    {
        ArgumentNullException.ThrowIfNull(detailMap);
        if (detailMap.Size != Size)
        {
            throw new ArgumentException(
                "The detail map and boundary-guidance field must have identical dimensions.",
                nameof(detailMap));
        }

        if (!double.IsFinite(contourReinforcement)
            || contourReinforcement < 0d
            || contourReinforcement > 4d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(contourReinforcement),
                contourReinforcement,
                "Contour reinforcement must be finite and between 0 and 4.");
        }

        float[] values = detailMap.CopyValues();
        if (contourReinforcement == 0d)
        {
            return new DetailMap(Size.Width, Size.Height, values);
        }

        for (int index = 0; index < values.Length; index++)
        {
            double reinforced = values[index]
                + (_subjectBoundary[index] * contourReinforcement * (1d - values[index]));
            values[index] = (float)Math.Clamp(reinforced, 0d, 1d);
        }

        return new DetailMap(Size.Width, Size.Height, values);
    }

    public static BoundaryGuidanceField Create(
        SceneBoundaryAnalysisResult analysis,
        BoundaryPaintingSettings settings,
        CancellationToken cancellationToken = default)
    {
        return CreateCore(
            analysis,
            settings,
            regionalField: null,
            cancellationToken);
    }

    public static BoundaryGuidanceField Create(
        SceneBoundaryAnalysisResult analysis,
        RegionSegmentationResult segmentation,
        BoundaryPaintingSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(segmentation);
        ArgumentNullException.ThrowIfNull(settings);
        if (analysis.EdgeImportanceMap.Size != segmentation.Labels.Size)
        {
            throw new ArgumentException(
                "The boundary analysis and regional segmentation must have identical dimensions.",
                nameof(segmentation));
        }

        RegionalBoundaryField regionalField = RegionalBoundaryField.Create(
            segmentation,
            RegionalBoundaryFieldSettings.FromBoundaryPainting(settings),
            cancellationToken);
        return CreateCore(
            analysis,
            settings,
            regionalField,
            cancellationToken);
    }

    private static BoundaryGuidanceField CreateCore(
        SceneBoundaryAnalysisResult analysis,
        BoundaryPaintingSettings settings,
        RegionalBoundaryField? regionalField,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(settings);

        ImageSize size = analysis.EdgeImportanceMap.Size;
        int length = checked((int)size.PixelCount);
        float[] edgeImportance = analysis.EdgeImportanceMap.CopyValues();
        float[] subjectBoundary = analysis.SubjectBoundaryMap.CopyValues();
        float[] internalStructure = analysis.InternalStructureMap.CopyValues();
        float[] textureEdges = analysis.TextureEdgeMap.CopyValues();
        float[] uncertainty = analysis.UncertaintyMap.CopyValues();
        BoundaryVector[] sourceTangents = analysis.DirectionField.CopyVectors();

        float[] influence = new float[length];
        float[] hardness = new float[length];
        float[] contour = new float[length];
        float[] corners = new float[length];
        BoundaryVector[] tangents = new BoundaryVector[length];
        float[] regionalStrength = new float[length];
        float[] regionalDistance = Enumerable.Repeat(float.PositiveInfinity, length).ToArray();
        BoundaryVector[] normals = new BoundaryVector[length];
        bool[] hardBarriers = new bool[length];

        for (int index = 0; index < length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            double internalSignal = internalStructure[index] * settings.InternalEdgeInfluence;
            double textureSignal = textureEdges[index] * settings.TextureEdgeInfluence;
            double structuralSignal = Math.Max(edgeImportance[index], internalSignal);
            double boundarySignal = Math.Max(subjectBoundary[index], Math.Max(structuralSignal, textureSignal));
            double uncertaintyProtection = 0.35d * uncertainty[index];

            influence[index] = (float)Math.Clamp(boundarySignal, 0d, 1d);
            hardness[index] = (float)Math.Clamp(
                Math.Max(subjectBoundary[index], structuralSignal * 0.82d) + uncertaintyProtection,
                0d,
                1d);
            contour[index] = subjectBoundary[index];
            tangents[index] = sourceTangents[index];
        }

        ComputeCornerStrength(
            influence,
            tangents,
            corners,
            size,
            cancellationToken);

        PropagateInfluence(
            influence,
            hardness,
            contour,
            corners,
            tangents,
            size,
            settings.AlignmentRadius,
            cancellationToken);

        if (regionalField is not null)
        {
            BlendRegionalField(
                regionalField,
                influence,
                hardness,
                contour,
                corners,
                tangents,
                regionalStrength,
                regionalDistance,
                normals,
                hardBarriers,
                size,
                cancellationToken);
        }

        return new BoundaryGuidanceField(
            size,
            influence,
            hardness,
            contour,
            corners,
            tangents,
            regionalStrength,
            regionalDistance,
            normals,
            hardBarriers);
    }

    private static void BlendRegionalField(
        RegionalBoundaryField regionalField,
        float[] influence,
        float[] hardness,
        float[] contour,
        float[] corners,
        BoundaryVector[] tangents,
        float[] regionalStrength,
        float[] regionalDistance,
        BoundaryVector[] normals,
        bool[] hardBarriers,
        ImageSize size,
        CancellationToken cancellationToken)
    {
        float[] regionalCorners = new float[influence.Length];
        for (int y = 0; y < size.Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (int x = 0; x < size.Width; x++)
            {
                int index = checked((y * size.Width) + x);
                RegionalBoundarySample sample = regionalField.Sample(x, y);
                regionalStrength[index] = (float)sample.BoundaryStrength;
                regionalDistance[index] = (float)sample.DistancePixels;
                normals[index] = sample.Normal;
                hardBarriers[index] = sample.IsHardBarrier;
                if (!sample.HasBoundary)
                {
                    continue;
                }

                float previousInfluence = influence[index];
                float regionalInfluence = (float)sample.Influence;
                influence[index] = Math.Max(previousInfluence, regionalInfluence);
                double regionalHardness = sample.IsHardBarrier
                    ? sample.Influence
                    : sample.Influence * (0.35d + (0.45d * sample.BoundaryStrength));
                hardness[index] = Math.Max(
                    hardness[index],
                    (float)Math.Clamp(regionalHardness, 0d, 1d));
                contour[index] = Math.Max(contour[index], regionalInfluence);
                if (sample.HasDirection
                    && (!tangents[index].IsDefined || regionalInfluence >= previousInfluence))
                {
                    tangents[index] = sample.Tangent;
                }
            }
        }

        ComputeCornerStrength(
            influence,
            tangents,
            regionalCorners,
            size,
            cancellationToken);
        for (int index = 0; index < corners.Length; index++)
        {
            corners[index] = Math.Max(corners[index], regionalCorners[index]);
        }
    }

    private static void ComputeCornerStrength(
        float[] influence,
        BoundaryVector[] tangents,
        float[] corners,
        ImageSize size,
        CancellationToken cancellationToken)
    {
        for (int y = 0; y < size.Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (int x = 0; x < size.Width; x++)
            {
                int index = checked((y * size.Width) + x);
                BoundaryVector tangent = tangents[index];
                if (!tangent.IsDefined || influence[index] <= 0f)
                {
                    continue;
                }

                double strongestTurn = 0d;
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    int neighborY = y + offsetY;
                    if (neighborY < 0 || neighborY >= size.Height)
                    {
                        continue;
                    }

                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        if (offsetX == 0 && offsetY == 0)
                        {
                            continue;
                        }

                        int neighborX = x + offsetX;
                        if (neighborX < 0 || neighborX >= size.Width)
                        {
                            continue;
                        }

                        int neighborIndex = checked((neighborY * size.Width) + neighborX);
                        BoundaryVector neighbor = tangents[neighborIndex];
                        if (!neighbor.IsDefined || influence[neighborIndex] <= 0f)
                        {
                            continue;
                        }

                        strongestTurn = Math.Max(
                            strongestTurn,
                            1d - Math.Abs(tangent.Dot(neighbor)));
                    }
                }

                corners[index] = (float)Math.Clamp(strongestTurn * influence[index], 0d, 1d);
            }
        }
    }

    private static void PropagateInfluence(
        float[] influence,
        float[] hardness,
        float[] contour,
        float[] corners,
        BoundaryVector[] tangents,
        ImageSize size,
        int radius,
        CancellationToken cancellationToken)
    {
        if (radius == 0)
        {
            return;
        }

        float decay = 1f / (radius + 1f);
        for (int iteration = 0; iteration < radius; iteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            float[] nextInfluence = (float[])influence.Clone();
            float[] nextHardness = (float[])hardness.Clone();
            float[] nextContour = (float[])contour.Clone();
            float[] nextCorners = (float[])corners.Clone();
            BoundaryVector[] nextTangents = (BoundaryVector[])tangents.Clone();

            for (int y = 0; y < size.Height; y++)
            {
                for (int x = 0; x < size.Width; x++)
                {
                    int index = checked((y * size.Width) + x);
                    for (int offsetY = -1; offsetY <= 1; offsetY++)
                    {
                        int neighborY = y + offsetY;
                        if (neighborY < 0 || neighborY >= size.Height)
                        {
                            continue;
                        }

                        for (int offsetX = -1; offsetX <= 1; offsetX++)
                        {
                            if (offsetX == 0 && offsetY == 0)
                            {
                                continue;
                            }

                            int neighborX = x + offsetX;
                            if (neighborX < 0 || neighborX >= size.Width)
                            {
                                continue;
                            }

                            int neighborIndex = checked((neighborY * size.Width) + neighborX);
                            float candidate = Math.Max(0f, influence[neighborIndex] - decay);
                            if (candidate <= nextInfluence[index])
                            {
                                continue;
                            }

                            nextInfluence[index] = candidate;
                            nextHardness[index] = Math.Max(0f, hardness[neighborIndex] - decay);
                            nextContour[index] = Math.Max(0f, contour[neighborIndex] - decay);
                            nextCorners[index] = Math.Max(0f, corners[neighborIndex] - decay);
                            nextTangents[index] = tangents[neighborIndex];
                        }
                    }
                }
            }

            Array.Copy(nextInfluence, influence, influence.Length);
            Array.Copy(nextHardness, hardness, hardness.Length);
            Array.Copy(nextContour, contour, contour.Length);
            Array.Copy(nextCorners, corners, corners.Length);
            Array.Copy(nextTangents, tangents, tangents.Length);
        }
    }
}
