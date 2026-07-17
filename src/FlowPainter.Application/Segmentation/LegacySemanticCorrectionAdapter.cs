using FlowPainter.Domain.Segmentation;
using FlowPainter.Domain.Semantics;

namespace FlowPainter.Application.Segmentation;

public static class LegacySemanticCorrectionAdapter
{
    public static IReadOnlyList<RegionRoleOverride> Convert(
        IEnumerable<SemanticCorrectionRegion> corrections)
    {
        ArgumentNullException.ThrowIfNull(corrections);

        RegionRoleOverride[] converted = corrections
            .Select(Convert)
            .ToArray();
        return Array.AsReadOnly(converted);
    }

    private static RegionRoleOverride Convert(SemanticCorrectionRegion correction)
    {
        ArgumentNullException.ThrowIfNull(correction);
        RegionRole role = correction.Kind switch
        {
            SemanticCorrectionKind.ForcePrimarySubject => RegionRole.Focal,
            SemanticCorrectionKind.ForceSubject => RegionRole.Subject,
            SemanticCorrectionKind.ForceBackground => RegionRole.Background,
            SemanticCorrectionKind.IgnoreAutomaticDetection => RegionRole.Ignore,
            _ => throw new ArgumentOutOfRangeException(
                nameof(correction),
                correction.Kind,
                "Unknown legacy semantic-correction kind.")
        };

        return new RegionRoleOverride(
            correction.Id,
            correction.Bounds,
            role,
            correction.Label,
            correction.SourceSemanticRegionId);
    }
}
