using System.Collections.ObjectModel;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Segmentation;

namespace FlowPainter.Application.Segmentation;

public sealed class RegionalStructureAnalysisResult
{
    private readonly ReadOnlyCollection<RegionRoleOverride> _roleOverrides;

    public RegionalStructureAnalysisResult(
        DetailMap structuralSaliencyMap,
        DetailMap protectionMap,
        DetailMap boundaryEvidenceMap,
        DetailMap focusMap,
        DetailMap importanceMap,
        DetailMap backgroundRoleMap,
        DetailMap ignoreMap,
        IEnumerable<RegionRoleOverride>? roleOverrides = null,
        string providerId = RegionalStructureAnalysisComposer.ProviderIdentifier)
    {
        ArgumentNullException.ThrowIfNull(structuralSaliencyMap);
        ArgumentNullException.ThrowIfNull(protectionMap);
        ArgumentNullException.ThrowIfNull(boundaryEvidenceMap);
        ArgumentNullException.ThrowIfNull(focusMap);
        ArgumentNullException.ThrowIfNull(importanceMap);
        ArgumentNullException.ThrowIfNull(backgroundRoleMap);
        ArgumentNullException.ThrowIfNull(ignoreMap);

        if (structuralSaliencyMap.Size != protectionMap.Size
            || structuralSaliencyMap.Size != boundaryEvidenceMap.Size
            || structuralSaliencyMap.Size != focusMap.Size
            || structuralSaliencyMap.Size != importanceMap.Size
            || structuralSaliencyMap.Size != backgroundRoleMap.Size
            || structuralSaliencyMap.Size != ignoreMap.Size)
        {
            throw new ArgumentException("All regional analysis maps must have identical dimensions.");
        }

        if (string.IsNullOrWhiteSpace(providerId))
        {
            throw new ArgumentException("A regional-analysis provider identifier is required.", nameof(providerId));
        }

        RegionRoleOverride[] copiedOverrides = roleOverrides?.ToArray() ?? [];
        HashSet<string> identifiers = new(StringComparer.OrdinalIgnoreCase);
        foreach (RegionRoleOverride roleOverride in copiedOverrides)
        {
            if (!identifiers.Add(roleOverride.Id))
            {
                throw new ArgumentException(
                    $"Duplicate region-role override identifier '{roleOverride.Id}'.",
                    nameof(roleOverrides));
            }
        }

        StructuralSaliencyMap = structuralSaliencyMap;
        ProtectionMap = protectionMap;
        BoundaryEvidenceMap = boundaryEvidenceMap;
        FocusMap = focusMap;
        ImportanceMap = importanceMap;
        BackgroundRoleMap = backgroundRoleMap;
        IgnoreMap = ignoreMap;
        _roleOverrides = Array.AsReadOnly(copiedOverrides);
        ProviderId = providerId.Trim();
    }

    public DetailMap StructuralSaliencyMap { get; }

    public DetailMap ProtectionMap { get; }

    public DetailMap BoundaryEvidenceMap { get; }

    public DetailMap FocusMap { get; }

    public DetailMap ImportanceMap { get; }

    public DetailMap BackgroundRoleMap { get; }

    public DetailMap IgnoreMap { get; }

    public IReadOnlyList<RegionRoleOverride> RoleOverrides => _roleOverrides;

    public string ProviderId { get; }

    public static RegionalStructureAnalysisResult CreateEmpty(
        FlowPainter.Domain.Images.ImageSize size,
        string providerId = RegionalStructureAnalysisComposer.ProviderIdentifier)
    {
        DetailMap empty = DetailMap.CreateUniform(size, 0f);
        return new RegionalStructureAnalysisResult(
            empty,
            empty,
            empty,
            empty,
            empty,
            empty,
            empty,
            providerId: providerId);
    }
}
