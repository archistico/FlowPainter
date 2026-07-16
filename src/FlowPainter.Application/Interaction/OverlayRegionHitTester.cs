using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Semantics;

namespace FlowPainter.Application.Interaction;

public static class OverlayRegionHitTester
{
    public static DetailRegion? SelectDetailRegion(
        IReadOnlyList<DetailRegion> regions,
        NormalizedPoint point,
        string? currentId = null)
    {
        ArgumentNullException.ThrowIfNull(regions);
        return SelectNext(
            regions
                .Reverse()
                .Where(region => region.Bounds.Contains(point))
                .ToArray(),
            currentId,
            region => region.Id);
    }

    public static SemanticCorrectionRegion? SelectSemanticCorrection(
        IReadOnlyList<SemanticCorrectionRegion> regions,
        NormalizedPoint point,
        string? currentId = null)
    {
        ArgumentNullException.ThrowIfNull(regions);
        return SelectNext(
            regions
                .Reverse()
                .Where(region => region.Bounds.Contains(point))
                .ToArray(),
            currentId,
            region => region.Id);
    }

    public static SemanticRegion? SelectAutomaticSemanticRegion(
        IReadOnlyList<SemanticRegion> regions,
        NormalizedPoint point,
        string? currentId = null)
    {
        ArgumentNullException.ThrowIfNull(regions);
        return SelectNext(
            regions
                .Where(region => region.Bounds.Contains(point))
                .OrderBy(region => region.Bounds.Width * region.Bounds.Height)
                .ThenByDescending(region => region.Role)
                .ThenBy(region => region.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            currentId,
            region => region.Id);
    }

    private static T? SelectNext<T>(
        IReadOnlyList<T> matches,
        string? currentId,
        Func<T, string> getId)
        where T : class
    {
        if (matches.Count == 0)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(currentId))
        {
            return matches[0];
        }

        int currentIndex = -1;
        for (int index = 0; index < matches.Count; index++)
        {
            if (string.Equals(getId(matches[index]), currentId, StringComparison.OrdinalIgnoreCase))
            {
                currentIndex = index;
                break;
            }
        }

        return matches[(currentIndex + 1) % matches.Count];
    }
}
