namespace FlowPainter.Application.Images;

public readonly record struct RenderMemoryEstimate(
    long SourceBytes,
    long OutputBytes,
    long AnalysisProxyBytes,
    long PreviewBytes)
{
    public long TotalRgbaBufferBytes => checked(SourceBytes + OutputBytes + AnalysisProxyBytes + PreviewBytes);

    public double TotalRgbaBufferMebibytes => TotalRgbaBufferBytes / 1024d / 1024d;
}
