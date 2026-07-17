namespace FlowPainter.Application.FlowPainting.Planning;

public readonly record struct LocalStrokeGeometry(
    double LengthMultiplier,
    double WidthMultiplier,
    double SegmentMultiplier,
    double CurveMultiplier)
{
    public static LocalStrokeGeometry Neutral { get; } = new(1d, 1d, 1d, 1d);
}
