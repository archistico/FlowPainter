using FlowPainter.Domain.Geometry;

namespace FlowPainter.Domain.Segmentation;

public sealed record RegionRoleOverride
{
    public RegionRoleOverride(
        string id,
        NormalizedRect bounds,
        RegionRole role,
        string? label = null,
        string? sourceRegionId = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A region-role override must have a non-empty identifier.", nameof(id));
        }

        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown region role.");
        }

        Id = id.Trim();
        Bounds = bounds;
        Role = role;
        Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
        SourceRegionId = string.IsNullOrWhiteSpace(sourceRegionId) ? null : sourceRegionId.Trim();
    }

    public string Id { get; }

    public NormalizedRect Bounds { get; }

    public RegionRole Role { get; }

    public string? Label { get; }

    public string? SourceRegionId { get; }
}
