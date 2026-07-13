using FlowPainter.Domain.Geometry;

namespace FlowPainter.Domain.Detail;

public sealed record DetailRegion
{
    public DetailRegion(
        string id,
        NormalizedRect bounds,
        double strength,
        DetailRegionOrigin origin,
        DetailRegionIntent intent,
        string? label = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A detail region must have a non-empty identifier.", nameof(id));
        }

        if (!double.IsFinite(strength) || strength < 0d || strength > 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(strength),
                strength,
                "Region strength must be a finite value between 0 and 1.");
        }

        Id = id.Trim();
        Bounds = bounds;
        Strength = strength;
        Origin = origin;
        Intent = intent;
        Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
    }

    public string Id { get; }

    public NormalizedRect Bounds { get; }

    public double Strength { get; }

    public DetailRegionOrigin Origin { get; }

    public DetailRegionIntent Intent { get; }

    public string? Label { get; }
}
