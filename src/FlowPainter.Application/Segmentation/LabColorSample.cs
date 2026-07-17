namespace FlowPainter.Application.Segmentation;

internal readonly record struct LabColorSample(
    double Lightness,
    double A,
    double B)
{
    public double DistanceTo(LabColorSample other)
    {
        double lightnessDifference = Lightness - other.Lightness;
        double aDifference = A - other.A;
        double bDifference = B - other.B;
        return Math.Sqrt(
            (lightnessDifference * lightnessDifference)
            + (aDifference * aDifference)
            + (bDifference * bDifference));
    }
}
