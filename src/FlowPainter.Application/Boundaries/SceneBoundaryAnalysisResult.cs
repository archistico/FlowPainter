using FlowPainter.Domain.Boundaries;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Images;

namespace FlowPainter.Application.Boundaries;

public sealed class SceneBoundaryAnalysisResult
{
    public SceneBoundaryAnalysisResult(
        DetailMap edgeStrengthMap,
        DetailMap edgeImportanceMap,
        DetailMap subjectBoundaryMap,
        DetailMap internalStructureMap,
        DetailMap textureEdgeMap,
        DetailMap backgroundConfidenceMap,
        DetailMap uncertaintyMap,
        BoundaryDirectionField directionField,
        string providerId = HeuristicSceneBoundaryAnalyzer.ProviderIdentifier)
    {
        ArgumentNullException.ThrowIfNull(edgeStrengthMap);
        ArgumentNullException.ThrowIfNull(edgeImportanceMap);
        ArgumentNullException.ThrowIfNull(subjectBoundaryMap);
        ArgumentNullException.ThrowIfNull(internalStructureMap);
        ArgumentNullException.ThrowIfNull(textureEdgeMap);
        ArgumentNullException.ThrowIfNull(backgroundConfidenceMap);
        ArgumentNullException.ThrowIfNull(uncertaintyMap);
        ArgumentNullException.ThrowIfNull(directionField);

        ImageSize size = edgeStrengthMap.Size;
        if (edgeImportanceMap.Size != size
            || subjectBoundaryMap.Size != size
            || internalStructureMap.Size != size
            || textureEdgeMap.Size != size
            || backgroundConfidenceMap.Size != size
            || uncertaintyMap.Size != size
            || directionField.Size != size)
        {
            throw new ArgumentException(
                "All scene-boundary maps and the direction field must have identical dimensions.",
                nameof(edgeImportanceMap));
        }

        if (string.IsNullOrWhiteSpace(providerId))
        {
            throw new ArgumentException("A boundary-analysis provider identifier is required.", nameof(providerId));
        }

        EdgeStrengthMap = edgeStrengthMap;
        EdgeImportanceMap = edgeImportanceMap;
        SubjectBoundaryMap = subjectBoundaryMap;
        InternalStructureMap = internalStructureMap;
        TextureEdgeMap = textureEdgeMap;
        BackgroundConfidenceMap = backgroundConfidenceMap;
        UncertaintyMap = uncertaintyMap;
        DirectionField = directionField;
        ProviderId = providerId.Trim();
    }

    public DetailMap EdgeStrengthMap { get; }

    public DetailMap EdgeImportanceMap { get; }

    public DetailMap SubjectBoundaryMap { get; }

    public DetailMap InternalStructureMap { get; }

    public DetailMap TextureEdgeMap { get; }

    public DetailMap BackgroundConfidenceMap { get; }

    public DetailMap UncertaintyMap { get; }

    public BoundaryDirectionField DirectionField { get; }

    public string ProviderId { get; }

    public static SceneBoundaryAnalysisResult CreateEmpty(
        ImageSize size,
        string providerId = HeuristicSceneBoundaryAnalyzer.ProviderIdentifier)
    {
        DetailMap empty = DetailMap.CreateUniform(size, 0f);
        return new SceneBoundaryAnalysisResult(
            empty,
            empty,
            empty,
            empty,
            empty,
            empty,
            empty,
            BoundaryDirectionField.CreateEmpty(size),
            providerId);
    }
}
