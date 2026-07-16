using FlowPainter.Domain.Geometry;

namespace FlowPainter.Domain.Semantics;

public sealed record SemanticCorrectionRegion
{
    public SemanticCorrectionRegion(
        string id,
        NormalizedRect bounds,
        SemanticCorrectionKind kind,
        string? label = null,
        string? sourceSemanticRegionId = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A semantic correction must have a non-empty identifier.", nameof(id));
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown semantic-correction kind.");
        }

        Id = id.Trim();
        Bounds = bounds;
        Kind = kind;
        Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
        SourceSemanticRegionId = string.IsNullOrWhiteSpace(sourceSemanticRegionId)
            ? null
            : sourceSemanticRegionId.Trim();
    }

    public string Id { get; }

    public NormalizedRect Bounds { get; }

    public SemanticCorrectionKind Kind { get; }

    public string? Label { get; }

    public string? SourceSemanticRegionId { get; }
}
