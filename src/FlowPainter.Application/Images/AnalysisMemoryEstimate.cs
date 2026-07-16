namespace FlowPainter.Application.Images;

public readonly record struct AnalysisMemoryEstimate(
    long SourceBytes,
    long ProxyRgbaBytes,
    long CurrentAnalysisBytes,
    long SegmentationReserveBytes)
{
    public long KnownPeakBytes => checked(
        SourceBytes
        + ProxyRgbaBytes
        + CurrentAnalysisBytes
        + SegmentationReserveBytes);

    public double KnownPeakMebibytes => KnownPeakBytes / 1024d / 1024d;
}
