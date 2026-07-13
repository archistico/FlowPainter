using FlowPainter.Domain.Geometry;

namespace FlowPainter.Domain.Semantics;

public sealed record SemanticRegion
{
    public SemanticRegion(
        string id,
        NormalizedRect bounds,
        double confidence,
        double importance,
        SemanticRegionRole role,
        SemanticSubjectKind kind = SemanticSubjectKind.Unknown,
        string? label = null,
        string? providerId = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A semantic region must have a non-empty identifier.", nameof(id));
        }

        ValidateUnitInterval(confidence, nameof(confidence));
        ValidateUnitInterval(importance, nameof(importance));

        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown semantic-region role.");
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown semantic subject kind.");
        }

        Id = id.Trim();
        Bounds = bounds;
        Confidence = confidence;
        Importance = importance;
        Role = role;
        Kind = kind;
        Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
        ProviderId = string.IsNullOrWhiteSpace(providerId) ? null : providerId.Trim();
    }

    public string Id { get; }

    public NormalizedRect Bounds { get; }

    public double Confidence { get; }

    public double Importance { get; }

    public SemanticRegionRole Role { get; }

    public SemanticSubjectKind Kind { get; }

    public string? Label { get; }

    public string? ProviderId { get; }

    private static void ValidateUnitInterval(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d || value > 1d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The value must be finite and between 0 and 1.");
        }
    }
}
