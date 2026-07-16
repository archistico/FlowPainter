using System.Collections.ObjectModel;
using FlowPainter.Application.Background;
using FlowPainter.Application.Boundaries;
using FlowPainter.Application.FlowPainting.Fields;
using FlowPainter.Domain.Brushes;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Domain.FlowFields;
using FlowPainter.Domain.Strokes;

namespace FlowPainter.Application.FlowPainting.Presets;

public static class BuiltInFlowPainterPresets
{
    private static readonly ReadOnlyCollection<FlowPainterPreset> Presets = Array.AsReadOnly(
        new FlowPainterPreset[]
        {
        new FlowPainterPreset(
            "Balanced",
            new FlowPainterSettings(
                field: new FlowFieldSettings(
                    FlowFieldKind.CoherentNoise,
                    scale: 3.5d,
                    octaves: 4,
                    persistence: 0.55d,
                    lacunarity: 2d),
                strokeCount: 12_000,
                segmentCount: 20,
                uniformDensity: 18d,
                lengthScale: 0.005d,
                maximumCurveRadians: 0.5d,
                minimumStrokeWidthPixels: 3d,
                maximumStrokeWidthPixels: 7d,
                strokeOpacity: 0.85d,
                brush: new BrushSettings(
                    BrushKind.SoftRound,
                    hardness: 0.78d,
                    sizeJitter: 0.03d,
                    opacityJitter: 0.03d),
                boundaryPainting: new BoundaryPaintingSettings(enabled: true),
                backgroundSuppression: new BackgroundSuppressionSettings(enabled: true))),
        new FlowPainterPreset(
            "Fine detail",
            new FlowPainterSettings(
                field: new FlowFieldSettings(
                    FlowFieldKind.CoherentNoise,
                    scale: 5.5d,
                    octaves: 5,
                    persistence: 0.5d,
                    lacunarity: 2d),
                strokeCount: 28_000,
                segmentCount: 28,
                uniformDensity: 14d,
                lengthScale: 0.004d,
                maximumCurveRadians: 0.42d,
                minimumStrokeWidthPixels: 1.5d,
                maximumStrokeWidthPixels: 4d,
                strokeOpacity: 0.72d,
                detailInfluence: new DetailInfluenceSettings(
                    placementBias: 6d,
                    detailedLengthMultiplier: 0.45d,
                    backgroundLengthMultiplier: 1.25d,
                    detailedWidthMultiplier: 0.55d,
                    backgroundWidthMultiplier: 1.3d),
                brush: new BrushSettings(
                    BrushKind.SolidRound,
                    hardness: 0.95d,
                    sizeJitter: 0.04d,
                    opacityJitter: 0.06d),
                boundaryPainting: new BoundaryPaintingSettings(
                    enabled: true,
                    tangentAlignment: 0.9d,
                    crossingPenalty: 0.92d,
                    terminationStrength: 0.88d,
                    contourReinforcement: 0.8d,
                    cornerPreservation: 0.82d))),
        new FlowPainterPreset(
            "Expressive",
            new FlowPainterSettings(
                field: new FlowFieldSettings(
                    FlowFieldKind.CoherentNoise,
                    scale: 2.2d,
                    octaves: 3,
                    persistence: 0.62d,
                    lacunarity: 2.15d,
                    angleOffsetRadians: 0.15d),
                strokeCount: 8_000,
                segmentCount: 16,
                uniformDensity: 22d,
                lengthScale: 0.008d,
                maximumCurveRadians: 0.85d,
                minimumStrokeWidthPixels: 4d,
                maximumStrokeWidthPixels: 10d,
                strokeOpacity: 0.65d,
                detailInfluence: new DetailInfluenceSettings(
                    placementBias: 3d,
                    detailedLengthMultiplier: 0.7d,
                    backgroundLengthMultiplier: 1.6d,
                    detailedWidthMultiplier: 0.8d,
                    backgroundWidthMultiplier: 1.7d),
                brush: new BrushSettings(
                    BrushKind.Flat,
                    hardness: 0.85d,
                    sizeJitter: 0.18d,
                    opacityJitter: 0.16d),
                boundaryPainting: new BoundaryPaintingSettings(
                    enabled: true,
                    tangentAlignment: 0.58d,
                    crossingPenalty: 0.48d,
                    terminationStrength: 0.35d,
                    textureEdgeInfluence: 0.04d,
                    contourReinforcement: 0.3d,
                    cornerPreservation: 0.45d))),
        new FlowPainterPreset(
            "Soft contour",
            new FlowPainterSettings(
                field: new FlowFieldSettings(
                    FlowFieldKind.CoherentNoise,
                    scale: 3.2d,
                    octaves: 4,
                    persistence: 0.55d,
                    lacunarity: 2d),
                strokeCount: 11_000,
                segmentCount: 20,
                uniformDensity: 18d,
                lengthScale: 0.0055d,
                maximumCurveRadians: 0.58d,
                minimumStrokeWidthPixels: 3d,
                maximumStrokeWidthPixels: 8d,
                strokeOpacity: 0.78d,
                brush: new BrushSettings(
                    BrushKind.SoftRound,
                    hardness: 0.68d,
                    sizeJitter: 0.05d,
                    opacityJitter: 0.05d),
                boundaryPainting: new BoundaryPaintingSettings(
                    enabled: true,
                    tangentAlignment: 0.48d,
                    crossingPenalty: 0.4d,
                    hardBoundaryThreshold: 0.72d,
                    terminationStrength: 0.25d,
                    contourReinforcement: 0.35d,
                    cornerPreservation: 0.42d))),
        new FlowPainterPreset(
            "Strong silhouette",
            new FlowPainterSettings(
                field: new FlowFieldSettings(
                    FlowFieldKind.CoherentNoise,
                    scale: 4.2d,
                    octaves: 5,
                    persistence: 0.5d,
                    lacunarity: 2d),
                strokeCount: 18_000,
                segmentCount: 24,
                uniformDensity: 16d,
                lengthScale: 0.0045d,
                maximumCurveRadians: 0.44d,
                minimumStrokeWidthPixels: 2d,
                maximumStrokeWidthPixels: 5.5d,
                strokeOpacity: 0.8d,
                detailInfluence: new DetailInfluenceSettings(
                    placementBias: 6.5d,
                    detailedLengthMultiplier: 0.48d,
                    backgroundLengthMultiplier: 1.4d,
                    detailedWidthMultiplier: 0.58d,
                    backgroundWidthMultiplier: 1.45d),
                brush: new BrushSettings(
                    BrushKind.SolidRound,
                    hardness: 0.92d,
                    sizeJitter: 0.03d,
                    opacityJitter: 0.04d),
                boundaryPainting: new BoundaryPaintingSettings(
                    enabled: true,
                    tangentAlignment: 0.96d,
                    alignmentRadius: 7,
                    crossingPenalty: 0.96d,
                    hardBoundaryThreshold: 0.48d,
                    terminationStrength: 0.95d,
                    internalEdgeInfluence: 0.5d,
                    textureEdgeInfluence: 0.03d,
                    contourReinforcement: 1.15d,
                    cornerPreservation: 0.9d))),
        new FlowPainterPreset(
            "Loose background",
            new FlowPainterSettings(
                field: new FlowFieldSettings(
                    FlowFieldKind.CoherentNoise,
                    scale: 2.4d,
                    octaves: 3,
                    persistence: 0.62d,
                    lacunarity: 2.1d),
                strokeCount: 8_500,
                segmentCount: 17,
                uniformDensity: 22d,
                lengthScale: 0.008d,
                maximumCurveRadians: 0.78d,
                minimumStrokeWidthPixels: 5d,
                maximumStrokeWidthPixels: 12d,
                strokeOpacity: 0.68d,
                detailInfluence: new DetailInfluenceSettings(
                    placementBias: 4.5d,
                    detailedLengthMultiplier: 0.6d,
                    backgroundLengthMultiplier: 1.7d,
                    detailedWidthMultiplier: 0.7d,
                    backgroundWidthMultiplier: 1.8d),
                brush: new BrushSettings(
                    BrushKind.Flat,
                    hardness: 0.82d,
                    sizeJitter: 0.16d,
                    opacityJitter: 0.14d),
                boundaryPainting: new BoundaryPaintingSettings(
                    enabled: true,
                    tangentAlignment: 0.7d,
                    alignmentRadius: 6,
                    crossingPenalty: 0.78d,
                    hardBoundaryThreshold: 0.58d,
                    terminationStrength: 0.62d,
                    internalEdgeInfluence: 0.3d,
                    textureEdgeInfluence: 0.02d,
                    contourReinforcement: 0.7d,
                    cornerPreservation: 0.58d),
                backgroundSuppression: new BackgroundSuppressionSettings(
                    enabled: true,
                    overallStrength: 0.92d,
                    detailFloor: 0.12d,
                    uncertaintyProtection: 0.92d,
                    silhouetteProtection: 1d,
                    transitionSoftness: 0.82d,
                    backgroundPlacementWeight: 0.2d,
                    strokeLengthMultiplier: 2.15d,
                    strokeWidthMultiplier: 1.9d,
                    segmentMultiplier: 0.48d,
                    curveFreedomMultiplier: 1.8d,
                    colorSimplification: 0.45d))),
        new FlowPainterPreset(
            "Legacy comparison",
            new FlowPainterSettings(
                field: new FlowFieldSettings(
                    FlowFieldKind.LegacyTrigonometric,
                    scale: 1d,
                    octaves: 1,
                    persistence: 1d,
                    lacunarity: 1d),
                strokeCount: 12_000,
                segmentCount: 20,
                uniformDensity: 18d,
                lengthScale: 0.005d,
                maximumCurveRadians: 0.5d,
                minimumStrokeWidthPixels: 3d,
                maximumStrokeWidthPixels: 7d,
                strokeOpacity: 1d,
                backgroundMode: StrokePlanBackgroundMode.SourceImage,
                detailInfluence: new DetailInfluenceSettings(
                    placementBias: 0d,
                    detailedLengthMultiplier: 1d,
                    backgroundLengthMultiplier: 1d,
                    detailedWidthMultiplier: 1d,
                    backgroundWidthMultiplier: 1d),
                brush: new BrushSettings(BrushKind.SolidRound),
                boundaryPainting: new BoundaryPaintingSettings(enabled: false))),
        new FlowPainterPreset(
            "Bristle study",
            new FlowPainterSettings(
                field: new FlowFieldSettings(
                    FlowFieldKind.CoherentNoise,
                    scale: 2.8d,
                    octaves: 4,
                    persistence: 0.58d,
                    lacunarity: 2.05d),
                strokeCount: 9_000,
                segmentCount: 18,
                uniformDensity: 20d,
                lengthScale: 0.007d,
                maximumCurveRadians: 0.7d,
                minimumStrokeWidthPixels: 5d,
                maximumStrokeWidthPixels: 12d,
                strokeOpacity: 0.72d,
                detailInfluence: new DetailInfluenceSettings(
                    placementBias: 4d,
                    detailedLengthMultiplier: 0.65d,
                    backgroundLengthMultiplier: 1.45d,
                    detailedWidthMultiplier: 0.72d,
                    backgroundWidthMultiplier: 1.55d),
                brush: new BrushSettings(
                    BrushKind.Bristle,
                    hardness: 0.52d,
                    sizeJitter: 0.12d,
                    opacityJitter: 0.18d,
                    bristleCount: 7,
                    bristleSpread: 0.82d),
                boundaryPainting: new BoundaryPaintingSettings(
                    enabled: true,
                    tangentAlignment: 0.7d,
                    crossingPenalty: 0.72d,
                    terminationStrength: 0.58d,
                    contourReinforcement: 0.48d)))
        });

    public static IReadOnlyList<FlowPainterPreset> All => Presets;
}
