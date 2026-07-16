using System.Globalization;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Semantics;

namespace FlowPainter.Application.Workflow;

public sealed class SemanticCorrectionRegionEditor
{
    private const double MinimumRegionSize = 0.002d;
    private readonly List<SemanticCorrectionRegion> _regions = [];
    private readonly IReadOnlyList<SemanticCorrectionRegion> _readOnlyRegions;
    private int _nextRegionNumber = 1;

    public SemanticCorrectionRegionEditor()
    {
        _readOnlyRegions = _regions.AsReadOnly();
    }

    public IReadOnlyList<SemanticCorrectionRegion> Regions => _readOnlyRegions;

    public int Count => _regions.Count;

    public SemanticCorrectionRegion Add(
        NormalizedRect bounds,
        SemanticCorrectionKind kind,
        string? label = null,
        string? sourceSemanticRegionId = null)
    {
        ValidateRegionSize(bounds);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown semantic-correction kind.");
        }

        string? normalizedSourceId = string.IsNullOrWhiteSpace(sourceSemanticRegionId)
            ? null
            : sourceSemanticRegionId.Trim();
        int existingSourceIndex = normalizedSourceId is null
            ? -1
            : _regions.FindIndex(region => string.Equals(
                region.SourceSemanticRegionId,
                normalizedSourceId,
                StringComparison.OrdinalIgnoreCase));
        string? excludedPrimaryId = existingSourceIndex >= 0
            ? _regions[existingSourceIndex].Id
            : null;
        if (kind == SemanticCorrectionKind.ForcePrimarySubject)
        {
            DemoteExistingPrimarySubject(excludedPrimaryId);
        }

        if (existingSourceIndex >= 0)
        {
            SemanticCorrectionRegion current = _regions[existingSourceIndex];
            SemanticCorrectionRegion replacement = new(
                current.Id,
                bounds,
                kind,
                string.IsNullOrWhiteSpace(label) ? current.Label : label,
                normalizedSourceId);
            _regions[existingSourceIndex] = replacement;
            return replacement;
        }

        int regionNumber = _nextRegionNumber++;
        SemanticCorrectionRegion region = new(
            $"semantic-correction-{regionNumber:D4}",
            bounds,
            kind,
            string.IsNullOrWhiteSpace(label) ? CreateDefaultLabel(kind, regionNumber) : label,
            normalizedSourceId);
        _regions.Add(region);
        return region;
    }

    public void ReplaceAll(IEnumerable<SemanticCorrectionRegion> regions)
    {
        ArgumentNullException.ThrowIfNull(regions);
        SemanticCorrectionRegion[] replacement = regions.ToArray();
        HashSet<string> identifiers = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> sourceIdentifiers = new(StringComparer.OrdinalIgnoreCase);
        int primarySubjectCount = 0;
        foreach (SemanticCorrectionRegion region in replacement)
        {
            ValidateRegionSize(region.Bounds);
            if (!identifiers.Add(region.Id))
            {
                throw new ArgumentException(
                    $"Duplicate semantic-correction identifier '{region.Id}'.",
                    nameof(regions));
            }

            if (region.SourceSemanticRegionId is not null
                && !sourceIdentifiers.Add(region.SourceSemanticRegionId))
            {
                throw new ArgumentException(
                    $"Duplicate semantic source-region identifier '{region.SourceSemanticRegionId}'.",
                    nameof(regions));
            }

            if (region.Kind == SemanticCorrectionKind.ForcePrimarySubject)
            {
                primarySubjectCount++;
            }
        }

        if (primarySubjectCount > 1)
        {
            throw new ArgumentException(
                "Only one semantic correction can force the primary subject.",
                nameof(regions));
        }

        _regions.Clear();
        _regions.AddRange(replacement);
        _nextRegionNumber = FindNextRegionNumber(replacement);
    }

    public SemanticCorrectionRegion Get(string id)
    {
        return _regions[FindIndex(id)];
    }

    public bool Remove(string id)
    {
        int index = _regions.FindIndex(region => string.Equals(region.Id, id, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return false;
        }

        _regions.RemoveAt(index);
        return true;
    }

    public void Clear()
    {
        _regions.Clear();
    }

    private void DemoteExistingPrimarySubject(string? excludedId)
    {
        int index = _regions.FindIndex(region =>
            region.Kind == SemanticCorrectionKind.ForcePrimarySubject
            && !string.Equals(region.Id, excludedId, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return;
        }

        SemanticCorrectionRegion current = _regions[index];
        _regions[index] = new SemanticCorrectionRegion(
            current.Id,
            current.Bounds,
            SemanticCorrectionKind.ForceSubject,
            current.Label,
            current.SourceSemanticRegionId);
    }

    private int FindIndex(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A semantic-correction identifier is required.", nameof(id));
        }

        int index = _regions.FindIndex(region => string.Equals(region.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));
        return index >= 0
            ? index
            : throw new KeyNotFoundException($"Semantic correction '{id}' was not found.");
    }

    private static string CreateDefaultLabel(SemanticCorrectionKind kind, int number)
    {
        return kind switch
        {
            SemanticCorrectionKind.ForcePrimarySubject => $"Primary subject {number}",
            SemanticCorrectionKind.ForceSubject => $"Subject {number}",
            SemanticCorrectionKind.ForceBackground => $"Background {number}",
            SemanticCorrectionKind.IgnoreAutomaticDetection => $"Ignored detection {number}",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown semantic-correction kind.")
        };
    }

    private static int FindNextRegionNumber(IEnumerable<SemanticCorrectionRegion> regions)
    {
        int maximum = 0;
        const string Prefix = "semantic-correction-";
        foreach (SemanticCorrectionRegion region in regions)
        {
            if (region.Id.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(
                    region.Id.AsSpan(Prefix.Length),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int number))
            {
                maximum = Math.Max(maximum, number);
            }
        }

        return maximum + 1;
    }

    private static void ValidateRegionSize(NormalizedRect bounds)
    {
        if (bounds.Width < MinimumRegionSize || bounds.Height < MinimumRegionSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bounds),
                bounds,
                $"The correction width and height must each be at least {MinimumRegionSize}.");
        }
    }
}
