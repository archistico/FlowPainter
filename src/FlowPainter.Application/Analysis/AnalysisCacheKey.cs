using System.Globalization;
using FlowPainter.Application.Background;
using FlowPainter.Application.Boundaries;
using FlowPainter.Application.Detail;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.Segmentation;
using FlowPainter.Application.Semantics;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Analysis;

public sealed record AnalysisCacheKey
{
    private AnalysisCacheKey(
        Guid sourceIdentity,
        ImageSize proxySize,
        long detailRegionRevision,
        long semanticCorrectionRevision,
        string settingsFingerprint)
    {
        if (sourceIdentity == Guid.Empty)
        {
            throw new ArgumentException("A non-empty source identity is required.", nameof(sourceIdentity));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(detailRegionRevision);
        ArgumentOutOfRangeException.ThrowIfNegative(semanticCorrectionRevision);
        if (string.IsNullOrWhiteSpace(settingsFingerprint))
        {
            throw new ArgumentException("An analysis settings fingerprint is required.", nameof(settingsFingerprint));
        }

        SourceIdentity = sourceIdentity;
        ProxySize = proxySize;
        DetailRegionRevision = detailRegionRevision;
        SemanticCorrectionRevision = semanticCorrectionRevision;
        SettingsFingerprint = settingsFingerprint;
    }

    public Guid SourceIdentity { get; }

    public ImageSize ProxySize { get; }

    public long DetailRegionRevision { get; }

    public long SemanticCorrectionRevision { get; }

    public string SettingsFingerprint { get; }

    public static AnalysisCacheKey Create(
        Guid sourceIdentity,
        ImageSize proxySize,
        DetailAnalysisSettings detailSettings,
        DetailInfluenceSettings detailInfluenceSettings,
        SemanticAnalysisSettings semanticSettings,
        SceneBoundaryAnalysisSettings boundarySettings,
        BackgroundSuppressionSettings backgroundSettings,
        long detailRegionRevision,
        long semanticCorrectionRevision,
        RegionSegmentationSettings? segmentationSettings = null,
        RegionMergeSettings? mergeSettings = null)
    {
        ArgumentNullException.ThrowIfNull(detailSettings);
        ArgumentNullException.ThrowIfNull(detailInfluenceSettings);
        ArgumentNullException.ThrowIfNull(semanticSettings);
        ArgumentNullException.ThrowIfNull(boundarySettings);
        ArgumentNullException.ThrowIfNull(backgroundSettings);
        segmentationSettings ??= new RegionSegmentationSettings();
        mergeSettings ??= new RegionMergeSettings();

        string fingerprint = string.Join(
            "|",
            Format(detailSettings.BaseDetail),
            Format(detailSettings.EdgeWeight),
            Format(detailSettings.ContrastWeight),
            detailSettings.SmoothingRadius.ToString(CultureInfo.InvariantCulture),
            Format(detailInfluenceSettings.RegionTransitionWidth),
            segmentationSettings.Enabled ? "1" : "0",
            segmentationSettings.TargetRegionSize.ToString(CultureInfo.InvariantCulture),
            Format(segmentationSettings.Compactness),
            Format(segmentationSettings.PreBlurSigma),
            segmentationSettings.MaximumIterations.ToString(CultureInfo.InvariantCulture),
            Format(segmentationSettings.ConvergenceTolerance),
            Format(mergeSettings.IntermediateTargetRatio),
            Format(mergeSettings.BroadMassTargetRatio),
            Format(mergeSettings.IntermediateMaximumCost),
            Format(mergeSettings.BroadMassMaximumCost),
            Format(mergeSettings.StrongBoundaryThreshold),
            Format(mergeSettings.MaximumParentAreaFraction),
            boundarySettings.Enabled ? "1" : "0",
            Format(boundarySettings.LuminanceWeight),
            Format(boundarySettings.ColorWeight),
            Format(boundarySettings.MultiscaleWeight),
            Format(boundarySettings.ContinuityWeight),
            Format(boundarySettings.SemanticBoundaryWeight),
            Format(boundarySettings.TextureSuppression),
            Format(boundarySettings.EdgeThreshold),
            Format(boundarySettings.ImportantEdgeThreshold),
            boundarySettings.CoarseRadius.ToString(CultureInfo.InvariantCulture),
            boundarySettings.SmoothingRadius.ToString(CultureInfo.InvariantCulture),
            boundarySettings.BoundaryProtectionRadius.ToString(CultureInfo.InvariantCulture),
            backgroundSettings.Enabled ? "1" : "0",
            Format(backgroundSettings.OverallStrength),
            Format(backgroundSettings.DetailFloor),
            Format(backgroundSettings.UncertaintyProtection),
            Format(backgroundSettings.SilhouetteProtection),
            Format(backgroundSettings.TransitionSoftness),
            Format(backgroundSettings.BackgroundPlacementWeight),
            Format(backgroundSettings.StrokeLengthMultiplier),
            Format(backgroundSettings.StrokeWidthMultiplier),
            Format(backgroundSettings.SegmentMultiplier),
            Format(backgroundSettings.CurveFreedomMultiplier),
            Format(backgroundSettings.ColorSimplification));

        return new AnalysisCacheKey(
            sourceIdentity,
            proxySize,
            detailRegionRevision,
            semanticCorrectionRevision,
            fingerprint);
    }

    private static string Format(double value)
    {
        return value.ToString("R", CultureInfo.InvariantCulture);
    }
}
