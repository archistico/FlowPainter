using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Semantics;

namespace FlowPainter.Application.Semantics;

public sealed class SemanticAnalysisResult
{
    private readonly IReadOnlyList<SemanticRegion> _regions;

    public SemanticAnalysisResult(
        DetailMap saliencyMap,
        DetailMap subjectMap,
        DetailMap silhouetteMap,
        DetailMap focalMap,
        DetailMap importanceMap,
        IReadOnlyList<SemanticRegion>? regions = null,
        string providerId = HeuristicSemanticImportanceAnalyzer.ProviderIdentifier)
    {
        ArgumentNullException.ThrowIfNull(saliencyMap);
        ArgumentNullException.ThrowIfNull(subjectMap);
        ArgumentNullException.ThrowIfNull(silhouetteMap);
        ArgumentNullException.ThrowIfNull(focalMap);
        ArgumentNullException.ThrowIfNull(importanceMap);

        if (subjectMap.Size != saliencyMap.Size
            || silhouetteMap.Size != saliencyMap.Size
            || focalMap.Size != saliencyMap.Size
            || importanceMap.Size != saliencyMap.Size)
        {
            throw new ArgumentException("All semantic maps must have identical dimensions.", nameof(subjectMap));
        }

        if (string.IsNullOrWhiteSpace(providerId))
        {
            throw new ArgumentException("A semantic provider identifier is required.", nameof(providerId));
        }

        SemanticRegion[] copiedRegions = regions?.ToArray() ?? [];
        HashSet<string> identifiers = new(StringComparer.OrdinalIgnoreCase);
        foreach (SemanticRegion region in copiedRegions)
        {
            if (!identifiers.Add(region.Id))
            {
                throw new ArgumentException(
                    $"Duplicate semantic-region identifier '{region.Id}'.",
                    nameof(regions));
            }
        }

        SaliencyMap = saliencyMap;
        SubjectMap = subjectMap;
        SilhouetteMap = silhouetteMap;
        FocalMap = focalMap;
        ImportanceMap = importanceMap;
        _regions = Array.AsReadOnly(copiedRegions);
        ProviderId = providerId.Trim();
    }

    public DetailMap SaliencyMap { get; }

    public DetailMap SubjectMap { get; }

    public DetailMap SilhouetteMap { get; }

    public DetailMap FocalMap { get; }

    public DetailMap ImportanceMap { get; }

    public IReadOnlyList<SemanticRegion> Regions => _regions;

    public string ProviderId { get; }

    public static SemanticAnalysisResult CreateEmpty(
        FlowPainter.Domain.Images.ImageSize size,
        string providerId = HeuristicSemanticImportanceAnalyzer.ProviderIdentifier)
    {
        DetailMap empty = DetailMap.CreateUniform(size, 0f);
        return new SemanticAnalysisResult(empty, empty, empty, empty, empty, providerId: providerId);
    }
}
