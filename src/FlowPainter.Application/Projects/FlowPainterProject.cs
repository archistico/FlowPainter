using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.Hybrid;
using FlowPainter.Application.PrimitiveGeneration;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Generation;
using FlowPainter.Domain.Semantics;

namespace FlowPainter.Application.Projects;

public sealed class FlowPainterProject
{
    private readonly IReadOnlyList<DetailRegion> _detailRegions;
    private readonly IReadOnlyList<SemanticCorrectionRegion> _semanticCorrections;

    public FlowPainterProject(
        string name,
        string sourcePath,
        ulong seed,
        FlowPainterSettings settings,
        PreviewSettings? preview = null,
        IReadOnlyList<DetailRegion>? detailRegions = null,
        FinalRenderSettings? finalRender = null,
        GenerativeMode mode = GenerativeMode.FlowPainting,
        PrimitiveGenerationSettings? primitiveGeneration = null,
        HybridGenerationSettings? hybridGeneration = null,
        IReadOnlyList<SemanticCorrectionRegion>? semanticCorrections = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A project must have a non-empty name.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("A project must reference a source image.", nameof(sourcePath));
        }

        ArgumentNullException.ThrowIfNull(settings);
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown generative mode.");
        }

        Name = name.Trim();
        SourcePath = sourcePath.Trim();
        Seed = seed;
        Settings = settings;
        Preview = preview ?? new PreviewSettings();
        FinalRender = finalRender ?? new FinalRenderSettings();
        Mode = mode;
        PrimitiveGeneration = primitiveGeneration ?? new PrimitiveGenerationSettings();
        HybridGeneration = hybridGeneration ?? new HybridGenerationSettings();
        DetailRegion[] copiedRegions = detailRegions?.ToArray() ?? [];

        HashSet<string> identifiers = new(StringComparer.OrdinalIgnoreCase);
        foreach (DetailRegion region in copiedRegions)
        {
            if (!identifiers.Add(region.Id))
            {
                throw new ArgumentException(
                    $"Duplicate detail-region identifier '{region.Id}'.",
                    nameof(detailRegions));
            }
        }

        _detailRegions = Array.AsReadOnly(copiedRegions);

        SemanticCorrectionRegion[] copiedCorrections = semanticCorrections?.ToArray() ?? [];
        identifiers.Clear();
        HashSet<string> sourceIdentifiers = new(StringComparer.OrdinalIgnoreCase);
        int primarySubjectCount = 0;
        foreach (SemanticCorrectionRegion correction in copiedCorrections)
        {
            if (!identifiers.Add(correction.Id))
            {
                throw new ArgumentException(
                    $"Duplicate semantic-correction identifier '{correction.Id}'.",
                    nameof(semanticCorrections));
            }

            if (correction.SourceSemanticRegionId is not null
                && !sourceIdentifiers.Add(correction.SourceSemanticRegionId))
            {
                throw new ArgumentException(
                    $"Duplicate semantic source-region identifier '{correction.SourceSemanticRegionId}'.",
                    nameof(semanticCorrections));
            }

            if (correction.Kind == SemanticCorrectionKind.ForcePrimarySubject)
            {
                primarySubjectCount++;
            }
        }

        if (primarySubjectCount > 1)
        {
            throw new ArgumentException(
                "Only one semantic correction can force the primary subject.",
                nameof(semanticCorrections));
        }

        _semanticCorrections = Array.AsReadOnly(copiedCorrections);
    }

    public string Name { get; }

    public string SourcePath { get; }

    public ulong Seed { get; }

    public FlowPainterSettings Settings { get; }

    public PreviewSettings Preview { get; }

    public FinalRenderSettings FinalRender { get; }

    public GenerativeMode Mode { get; }

    public PrimitiveGenerationSettings PrimitiveGeneration { get; }

    public HybridGenerationSettings HybridGeneration { get; }

    public IReadOnlyList<DetailRegion> DetailRegions => _detailRegions;

    public IReadOnlyList<SemanticCorrectionRegion> SemanticCorrections => _semanticCorrections;
}
