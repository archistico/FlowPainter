using System.Collections.ObjectModel;
using FlowPainter.Application.FlowPainting.Fields;
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
                strokeOpacity: 0.85d)),
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
                    backgroundWidthMultiplier: 1.3d))),
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
                    backgroundWidthMultiplier: 1.7d))),
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
                    backgroundWidthMultiplier: 1d)))
        });

    public static IReadOnlyList<FlowPainterPreset> All => Presets;
}
