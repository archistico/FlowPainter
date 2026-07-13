namespace FlowPainter.Application.Images;

public readonly record struct FinalRenderMemoryEstimate(
    long SourceBytes,
    long AnalysisProxyBytes,
    long PreviewBytes,
    long DetailOverlayBytes,
    long OutputSurfaceBytes,
    long OutputCopyBytes)
{
    public const long ElevatedThresholdBytes = 768L * 1024L * 1024L;
    public const long HighThresholdBytes = 1536L * 1024L * 1024L;

    public long OutputWorkingBytes => checked(OutputSurfaceBytes + OutputCopyBytes);

    public long KnownPeakBytes => checked(
        SourceBytes
        + AnalysisProxyBytes
        + PreviewBytes
        + DetailOverlayBytes
        + OutputWorkingBytes);

    public double KnownPeakMebibytes => KnownPeakBytes / 1024d / 1024d;

    public FinalRenderMemoryRisk Risk => KnownPeakBytes switch
    {
        >= HighThresholdBytes => FinalRenderMemoryRisk.High,
        >= ElevatedThresholdBytes => FinalRenderMemoryRisk.Elevated,
        _ => FinalRenderMemoryRisk.Normal
    };
}
